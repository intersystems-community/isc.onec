/*
	 using _1C.V8.Data;
	 
	 V8DbConnection conn = new V8DbConnection("File=c:/info_base", "", ""))
     conn.Open();
*/

[ToolboxBitmap(typeof(V8DbConnection), "Images.V8DbConnection.bmp")]
public class V8DbConnection : Component, IDbConnection, IDisposable, ICloneable
{
    // Fields
    internal static readonly object EventStateChange = new object();
    private ComObject m_connection;
    private string m_connectionString;
    private string m_database;
    private string m_IBAlias;
    private string m_password;
    private string m_user;
    private static ComObject s_connector = null;
    private static ReaderWriterLock s_connectorLock = new ReaderWriterLock();
    private static int s_maxConnections = 0;
    private static int s_poolCapacity = 0;
    private static int s_poolTimeout = 0;
    private static ComConnectorVersion s_version = ComConnectorVersion.Ver8_0;

    // Events
    [Description("Событие, возникающие при изменении состояния соединения.")]
    public event StateChangeEventHandler StateChange
    {
        add
        {
            base.Events.AddHandler(EventStateChange, value);
        }
        remove
        {
            base.Events.RemoveHandler(EventStateChange, value);
        }
    }

    // Methods
    public V8DbConnection()
    {
        this.m_connection = null;
        this.m_connectionString = string.Empty;
        this.m_database = string.Empty;
        this.m_user = string.Empty;
        this.m_password = string.Empty;
        this.m_IBAlias = string.Empty;
    }

    public V8DbConnection(string connectionString)
    {
        this.m_connection = null;
        this.m_connectionString = string.Empty;
        this.m_database = string.Empty;
        this.m_user = string.Empty;
        this.m_password = string.Empty;
        this.m_IBAlias = string.Empty;
        this.ConnectionString = connectionString;
    }

    public V8DbConnection(string database, string user, string password)
    {
        this.m_connection = null;
        this.m_connectionString = string.Empty;
        this.m_database = string.Empty;
        this.m_user = string.Empty;
        this.m_password = string.Empty;
        this.m_IBAlias = string.Empty;
        this.m_database = database;
        if (this.m_database == null)
        {
            this.m_database = string.Empty;
        }
        this.m_user = user;
        this.m_password = password;
        ConnectionStringBuilder builder = new ConnectionStringBuilder(this.m_database);
        builder["Usr"] = this.m_user;
        builder["Pwd"] = this.m_password;
        this.m_connectionString = builder.ToString();
    }

    public IDbTransaction BeginTransaction()
    {
        return new V8Transaction(this);
    }

    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        return new V8Transaction(this, isolationLevel);
    }

    public void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException();
    }

    public static void ClearPool()
    {
        try
        {
            s_connectorLock.AcquireWriterLock(-1);
            if (s_connector != null)
            {
                s_connector.Dispose();
                s_connector = null;
            }
        }
        finally
        {
            s_connectorLock.ReleaseWriterLock();
        }
    }

    public object Clone()
    {
        return base.MemberwiseClone();
    }

    public void Close()
    {
        if (this.m_connection != null)
        {
            this.m_connection.Dispose();
            this.m_connection = null;
            this.OnStateChange(ConnectionState.Open, ConnectionState.Closed);
        }
    }

    public IDbCommand CreateCommand()
    {
        V8DbCommand command = new V8DbSelectCommand();
        command.Connection = this;
        return command;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            this.Close();
        }
    }

    ~V8DbConnection()
    {
        this.Dispose(false);
    }

    private static string GetComConnectorName()
    {
        if (Version == ComConnectorVersion.Ver8_0)
        {
            return "V8.ComConnector";
        }
        if (Version == ComConnectorVersion.Ver8_1)
        {
            return "V81.ComConnector";
        }
        return "V82.ComConnector";
    }

    internal void GetConnection()
    {
    Label_0000:
        if (s_poolCapacity == 0)
        {
            try
            {
                s_connectorLock.AcquireReaderLock(-1);
                if (s_poolCapacity != 0)
                {
                    goto Label_0000;
                }
                using (ComObject obj2 = new ComObject(Activator.CreateInstance(Type.GetTypeFromProgID(GetComConnectorName()))))
                {
                    this.m_connection = new ComObject(V8.Call(obj2, "connect", new object[] { this.ConnectionString }));
                }
                return;
            }
            finally
            {
                s_connectorLock.ReleaseReaderLock();
            }
        }
        if (s_connector == null)
        {
            try
            {
                s_connectorLock.AcquireWriterLock(10);
                try
                {
                    if ((s_poolCapacity == 0) || (s_connector != null))
                    {
                        goto Label_0000;
                    }
                    s_connector = new ComObject(Activator.CreateInstance(Type.GetTypeFromProgID(GetComConnectorName())));
                    V8.Put(s_connector, "PoolCapacity", s_poolCapacity);
                    V8.Put(s_connector, "PoolTimeout", s_poolTimeout);
                    V8.Put(s_connector, "MaxConnections", s_maxConnections);
                }
                finally
                {
                    s_connectorLock.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                goto Label_0000;
            }
        }
        try
        {
            s_connectorLock.AcquireReaderLock(-1);
            if ((s_poolCapacity == 0) || (s_connector == null))
            {
                goto Label_0000;
            }
            this.m_connection = new ComObject(V8.Call(s_connector, "connect", new object[] { this.ConnectionString }));
        }
        finally
        {
            s_connectorLock.ReleaseReaderLock();
        }
    }

    private void OnStateChange(ConnectionState original, ConnectionState state)
    {
        StateChangeEventHandler handler = (StateChangeEventHandler) base.Events[EventStateChange];
        if (handler != null)
        {
            handler(this, new StateChangeEventArgs(original, state));
        }
    }

    public void Open()
    {
        if (this.m_connection == null)
        {
            this.GetConnection();
            if (this.m_connection != null)
            {
                this.OnStateChange(ConnectionState.Closed, ConnectionState.Open);
            }
        }
    }

    void IDisposable.Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal string XMLTypeNameOfValue(object value)
    {
        if (value is string)
        {
            return typeof(string).Name;
        }
        if (((value is int) || (value is double)) || (value is decimal))
        {
            return typeof(decimal).Name;
        }
        if (value is DateTime)
        {
            return typeof(DateTime).Name;
        }
        if (value is bool)
        {
            return typeof(bool).Name;
        }
        using (ComObject obj2 = new ComObject(V8.Call(this.m_connection, "XMLTypeOf", new object[] { value })))
        {
            if (obj2.comObject == null)
            {
                return string.Empty;
            }
            return (string) V8.Get(obj2, "TypeName");
        }
    }

    // Properties
    [DefaultValue((string) null), Browsable(false)]
    public ComObject Connection
    {
        get
        {
            return this.m_connection;
        }
    }

    [Editor(typeof(V8ConnectionStringUITypeEditor), typeof(UITypeEditor)), Description("Строка соединения к ИБ"), Category("Data"), DefaultValue("")]
    public string ConnectionString
    {
        get
        {
            return this.m_connectionString;
        }
        set
        {
            this.m_connectionString = value;
            if (this.m_connectionString == null)
            {
                this.m_connectionString = string.Empty;
            }
            ConnectionStringBuilder builder = new ConnectionStringBuilder(this.m_connectionString);
            this.m_user = builder["Usr"];
            this.m_password = builder["Pwd"];
            builder["Usr"] = null;
            builder["Pwd"] = null;
            this.m_database = builder.ToString();
            if (this.m_database == null)
            {
                this.m_database = string.Empty;
            }
        }
    }

    [Browsable(false)]
    public int ConnectionTimeout
    {
        get
        {
            return 0;
        }
    }

    [Description("Имя информационной базы"), Category("Data"), DefaultValue("")]
    public string Database
    {
        get
        {
            return this.m_database;
        }
        set
        {
            this.m_database = value;
            if (this.m_database == null)
            {
                this.m_database = string.Empty;
            }
            ConnectionStringBuilder builder = new ConnectionStringBuilder(this.m_database);
            builder["Usr"] = this.m_user;
            builder["Pwd"] = this.m_password;
            this.m_connectionString = builder.ToString();
        }
    }

    [DefaultValue("")]
    public string IBAlias
    {
        get
        {
            return this.m_IBAlias;
        }
        set
        {
            this.m_IBAlias = value;
        }
    }

    public static int MaxConnections
    {
        get
        {
            return s_maxConnections;
        }
        set
        {
            try
            {
                s_connectorLock.AcquireWriterLock(-1);
                s_maxConnections = value;
                if (s_maxConnections < 0)
                {
                    s_maxConnections = 0;
                }
                if (s_connector != null)
                {
                    V8.Put(s_connector, "PoolTimeout", s_maxConnections);
                }
            }
            finally
            {
                s_connectorLock.ReleaseWriterLock();
            }
        }
    }

    [DefaultValue(""), Description("Пароль"), Category("Data")]
    public string Password
    {
        get
        {
            return this.m_password;
        }
        set
        {
            this.m_password = value;
            ConnectionStringBuilder builder = new ConnectionStringBuilder(this.m_connectionString);
            builder["Pwd"] = this.m_password;
            this.m_connectionString = builder.ToString();
        }
    }

    public static int PoolCapacity
    {
        get
        {
            return s_poolCapacity;
        }
        set
        {
            try
            {
                s_connectorLock.AcquireWriterLock(-1);
                s_poolCapacity = value;
                if (s_connector != null)
                {
                    if (s_poolCapacity > 0)
                    {
                        V8.Put(s_connector, "PoolCapacity", s_poolTimeout);
                    }
                    else
                    {
                        s_poolCapacity = 0;
                        s_connector.Dispose();
                        s_connector = null;
                    }
                }
            }
            finally
            {
                s_connectorLock.ReleaseWriterLock();
            }
        }
    }

    public static int PoolTimeout
    {
        get
        {
            return s_poolTimeout;
        }
        set
        {
            try
            {
                s_connectorLock.AcquireWriterLock(-1);
                s_poolTimeout = value;
                if (s_poolTimeout < 0)
                {
                    s_poolTimeout = 0;
                }
                if (s_connector != null)
                {
                    V8.Put(s_connector, "PoolTimeout", s_poolTimeout);
                }
            }
            finally
            {
                s_connectorLock.ReleaseWriterLock();
            }
        }
    }

    [Browsable(false)]
    public ConnectionState State
    {
        get
        {
            if (this.m_connection != null)
            {
                return ConnectionState.Open;
            }
            return ConnectionState.Closed;
        }
    }

    [DefaultValue(""), Description("Имя пользователя"), Category("Data")]
    public string User
    {
        get
        {
            return this.m_user;
        }
        set
        {
            this.m_user = value;
            ConnectionStringBuilder builder = new ConnectionStringBuilder(this.m_connectionString);
            builder["Usr"] = this.m_user;
            this.m_connectionString = builder.ToString();
        }
    }

    public static ComConnectorVersion Version
    {
        get
        {
            return s_version;
        }
        set
        {
            if (s_version != value)
            {
                try
                {
                    s_connectorLock.AcquireWriterLock(-1);
                    s_version = value;
                    if (s_connector != null)
                    {
                        s_connector.Dispose();
                        s_connector = null;
                    }
                }
                finally
                {
                    s_connectorLock.ReleaseWriterLock();
                }
            }
        }
    }
}

 
/*

*/
 public ComObject(object o)
{
    this.m_comObject = null;
    this.m_comObject = o;
}

 
/*
*/

public class V8
{
    // Fields
    private static ResourceManager resourceManager = null;
    private const string StringResourceName = "_1C.V8.Data.strings";

    // Methods
    private V8()
    {
    }

    public static object Call(ComObject target, string methodName, params object[] methodParams)
    {
        object obj2;
        if (methodParams.Length > 0)
        {
            object[] objArray = new object[methodParams.Length];
            try
            {
                for (int i = 0; i < methodParams.Length; i++)
                {
                    objArray[i] = methodParams[i];
                }
                ParameterModifier modifier = new ParameterModifier(methodParams.Length);
                for (int j = 0; j < methodParams.Length; j++)
                {
                    modifier[j] = true;
                }
                ParameterModifier[] modifiers = new ParameterModifier[] { modifier };
                obj2 = target.comObject.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target.comObject, methodParams, modifiers, null, null);
            }
            finally
            {
                for (int k = 0; k < methodParams.Length; k++)
                {
                    if ((objArray[k] != null) && Marshal.IsComObject(objArray[k]))
                    {
                        Marshal.ReleaseComObject(objArray[k]);
                    }
                }
            }
        }
        else
        {
            obj2 = target.comObject.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target.comObject, methodParams);
        }
        RegisterComObject(obj2);
        return obj2;
    }

    public static object Call(V8DbConnection connection, ComObject target, string methodName, params object[] methodParams)
    {
        object obj4;
        object[] objArray = new object[methodParams.Length];
        try
        {
            for (int i = 0; i < methodParams.Length; i++)
            {
                objArray[i] = ConvertValueNetToV8(methodParams[i], connection);
            }
            object obj2 = Call(target, methodName, objArray);
            object obj3 = null;
            try
            {
                obj3 = ConvertValueV8ToNet(obj2, connection);
                for (int j = 0; j < methodParams.Length; j++)
                {
                    methodParams[j] = ConvertValueV8ToNet(objArray[j], connection);
                }
            }
            finally
            {
                if (((obj2 != null) && Marshal.IsComObject(obj2)) && !(obj3 is ComObject))
                {
                    ReleaseComObject(obj2);
                }
            }
            obj4 = obj3;
        }
        finally
        {
            for (int k = 0; k < methodParams.Length; k++)
            {
                if (((objArray[k] != null) && !Marshal.IsComObject(methodParams[k])) && (!(methodParams[k] is ComObject) && Marshal.IsComObject(objArray[k])))
                {
                    ReleaseComObject(objArray[k]);
                }
            }
        }
        return obj4;
    }

    public static object Call(V8DbConnection connection, ObjectRef target, string methodName, params object[] methodParams)
    {
        using (ComObject obj2 = target.Reference(connection))
        {
            return Call(connection, obj2, methodName, methodParams);
        }
    }

    public static object CallByString(V8DbConnection connection, string methodName, params object[] methodParams)
    {
        return CallByString(connection, connection.Connection, methodName, methodParams);
    }

    public static object CallByString(V8DbConnection connection, ComObject target, string methodName, params object[] methodParams)
    {
        object o = null;
        object obj5;
        object[] objArray = new object[methodParams.Length];
        try
        {
            for (int i = 0; i < methodParams.Length; i++)
            {
                objArray[i] = ConvertValueNetToV8(methodParams[i], connection);
            }
            object comObject = target.comObject;
            foreach (string str in methodName.Split(new char[] { '.' }))
            {
                object[] objArray2;
                if ((o != null) && Marshal.IsComObject(o))
                {
                    comObject = o;
                }
                if (!str.EndsWith(")"))
                {
                    goto Label_024D;
                }
                string name = str;
                string s = string.Empty;
                int index = name.IndexOf('(');
                if (index >= 0)
                {
                    s = name.Substring(index + 1, (name.Length - index) - 2).Trim();
                    name = name.Substring(0, index).Trim();
                }
                ArrayList list = new ArrayList();
                int num3 = 0;
                goto Label_020F;
            Label_00D6:
                num3++;
            Label_00DC:
                if ((num3 < s.Length) && char.IsWhiteSpace(s, num3))
                {
                    goto Label_00D6;
                }
                if ((num3 >= s.Length) || (s[num3] != '{'))
                {
                    goto Label_021D;
                }
                num3++;
                while ((num3 < s.Length) && char.IsWhiteSpace(s, num3))
                {
                    num3++;
                }
                string str4 = string.Empty;
                while ((num3 < s.Length) && char.IsDigit(s, num3))
                {
                    str4 = str4 + s[num3];
                    num3++;
                }
                while ((num3 < s.Length) && char.IsWhiteSpace(s, num3))
                {
                    num3++;
                }
                if ((num3 >= s.Length) || (s[num3] != '}'))
                {
                    goto Label_021D;
                }
                num3++;
                try
                {
                    int num4 = int.Parse(str4);
                    if ((num4 >= 0) && (num4 < objArray.Length))
                    {
                        list.Add(objArray[num4]);
                    }
                    goto Label_01DB;
                }
                catch
                {
                    goto Label_021D;
                }
            Label_01D5:
                num3++;
            Label_01DB:
                if ((num3 < s.Length) && char.IsWhiteSpace(s, num3))
                {
                    goto Label_01D5;
                }
                if ((num3 >= s.Length) || (s[num3] != ','))
                {
                    goto Label_021D;
                }
                num3++;
            Label_020F:
                if (num3 < s.Length)
                {
                    goto Label_00DC;
                }
            Label_021D:
                objArray2 = new object[list.Count];
                list.CopyTo(objArray2);
                o = comObject.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, comObject, objArray2);
                goto Label_0263;
            Label_024D:
                o = comObject.GetType().InvokeMember(str, BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, comObject, null);
            Label_0263:
                RegisterComObject(o);
                if (comObject != target.comObject)
                {
                    ReleaseComObject(comObject);
                    comObject = null;
                }
            }
            object obj4 = null;
            if (o != null)
            {
                try
                {
                    obj4 = ConvertValueV8ToNet(o, connection);
                }
                finally
                {
                    if (((o != null) && Marshal.IsComObject(o)) && !(obj4 is ComObject))
                    {
                        ReleaseComObject(o);
                    }
                }
            }
            obj5 = obj4;
        }
        finally
        {
            for (int j = 0; j < methodParams.Length; j++)
            {
                if (((objArray[j] != null) && !Marshal.IsComObject(methodParams[j])) && (!(methodParams[j] is ComObject) && Marshal.IsComObject(objArray[j])))
                {
                    ReleaseComObject(objArray[j]);
                }
            }
        }
        return obj5;
    }

    public static object CallByString(V8DbConnection connection, ObjectRef target, string methodName, params object[] methodParams)
    {
        using (ComObject obj2 = target.Reference(connection))
        {
            return CallByString(connection, obj2, methodName, methodParams);
        }
    }

    public static object ConvertValueNetToV8(object value, V8DbConnection connection)
    {
        if (value is ObjectRef)
        {
            if (value == Undefined.Value)
            {
                value = null;
                return value;
            }
            value = ((ObjectRef) value).Reference(connection.Connection).comObject;
            return value;
        }
        if (value is TypeDescription)
        {
            value = ((TypeDescription) value).GetTypeDescription(connection).comObject;
            return value;
        }
        if (value is V8SystemEnum)
        {
            value = ((V8SystemEnum) value).Reference(connection).comObject;
            return value;
        }
        if (value is Enum)
        {
            string str = string.Empty;
            string name = value.GetType().Name;
            XmlTypeAttribute attribute = (XmlTypeAttribute) TypeDescriptor.GetAttributes(value)[typeof(XmlTypeAttribute)];
            if (attribute != null)
            {
                str = attribute.Namespace;
                if (str == null)
                {
                    str = string.Empty;
                }
                name = attribute.TypeName;
            }
            using (ComObject obj2 = new ComObject(Call(connection.Connection, "NewObject", new object[] { "XMLDataType", name, str })))
            {
                using (ComObject obj3 = new ComObject(Call(connection.Connection, "FromXMLType", new object[] { obj2.comObject })))
                {
                    string str3 = value.ToString();
                    if (str3 == "EmptyRef")
                    {
                        str3 = string.Empty;
                    }
                    value = Call(connection.Connection, "XMLValue", new object[] { obj3.comObject, str3 });
                }
                return value;
            }
        }
        if (value is Array)
        {
            Array array = (Array) value;
            ComObject target = new ComObject(Call(connection.Connection, "NewObject", new object[] { "Array" }));
            foreach (object obj5 in array)
            {
                object obj6 = ConvertValueNetToV8(obj5, connection);
                Call(target, "Add", new object[] { obj6 });
                ReleaseComObject(obj6);
            }
            value = target.comObject;
            return value;
        }
        if (value is DateTime)
        {
            DateTime time = (DateTime) value;
            if (time.Year == 1)
            {
                time = (DateTime) value;
                if (time.Month != 1)
                {
                    return value;
                }
                time = (DateTime) value;
                if (time.Day == 1)
                {
                    value = new DateTime(100, 1, 1);
                }
            }
            return value;
        }
        if (value is int)
        {
            value = (int) value;
            return value;
        }
        if (value is double)
        {
            value = (decimal) ((double) value);
            return value;
        }
        if (value is ComObject)
        {
            value = ((ComObject) value).comObject;
        }
        return value;
    }

    public static object ConvertValueV8ToNet(object value, V8DbConnection connection)
    {
        V8TypeInfo typeInfo = null;
        if (value != null)
        {
            string xmlTypeName = connection.XMLTypeNameOfValue(value);
            if (xmlTypeName.Length != 0)
            {
                try
                {
                    typeInfo = V8Metadata.GetMetadata(connection.IBAlias).TypesInfo.GetByXmlName(xmlTypeName);
                }
                catch (FileNotFoundException)
                {
                    typeInfo = null;
                }
            }
        }
        return ConvertValueV8ToNet(value, connection, typeInfo);
    }

    public static object ConvertValueV8ToNet(object value, V8DbConnection connection, V8TypeInfo typeInfo)
    {
        if ((value != null) && !(value is DBNull))
        {
            if (typeInfo == null)
            {
                string strA = connection.XMLTypeNameOfValue(value);
                if (string.Compare(strA, V8Consts.kAccumCorrespondenceKind, true) == 0)
                {
                    string str2 = (string) Call(connection.Connection, "XMLString", new object[] { value });
                    value = Enum.Parse(typeof(AccumulationMovementType), str2);
                }
                else if (string.Compare(strA, "AccountType", true) == 0)
                {
                    string str3 = (string) Call(connection.Connection, "XMLString", new object[] { value });
                    value = Enum.Parse(typeof(AccountType), str3);
                }
                else if (string.Compare(strA, V8AccMDProvider.kCorrespondenceKind, true) == 0)
                {
                    string str4 = (string) Call(connection.Connection, "XMLString", new object[] { value });
                    value = Enum.Parse(typeof(AccountingMovementType), str4);
                }
            }
            else if (typeInfo.IsReference || typeInfo.IsEnum)
            {
                value = From1CValue(value, connection, typeInfo);
            }
            else if (typeInfo.Category == V8TypeCategory.TypeDescription)
            {
                using (ComObject obj2 = new ComObject(value))
                {
                    value = new TypeDescription(obj2, connection.Connection, V8Metadata.GetMetadata(connection.IBAlias));
                }
            }
        }
        if (value is double)
        {
            value = (decimal) ((double) value);
        }
        else if (value is int)
        {
            value = (int) value;
        }
        if ((value != null) && Marshal.IsComObject(value))
        {
            value = new ComObject(value);
        }
        return value;
    }

    public static object From1CValue(object value, V8DbConnection connection, V8TypeInfo typeInfo)
    {
        string val = (string) Call(connection.Connection, "XMLString", new object[] { value });
        return GetObject(typeInfo, val);
    }

    public static object FromInvariantString(string str, V8TypeInfo typeInfo)
    {
        if (str != null)
        {
            return GetObject(typeInfo, str);
        }
        return null;
    }

    public static object Get(ComObject target, string propertyName)
    {
        object obj2 = target.comObject.GetType().InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, target.comObject, null);
        RegisterComObject(obj2);
        return obj2;
    }

    public static object Get(V8DbConnection connection, ComObject target, string propertyName)
    {
        object obj2 = Get(target, propertyName);
        object obj3 = null;
        try
        {
            obj3 = ConvertValueV8ToNet(obj2, connection);
        }
        finally
        {
            if (((obj2 != null) && Marshal.IsComObject(obj2)) && !(obj3 is ComObject))
            {
                ReleaseComObject(obj2);
            }
        }
        return obj3;
    }

    public static object Get(V8DbConnection connection, ObjectRef target, string propertyName)
    {
        using (ComObject obj2 = target.Reference(connection))
        {
            return Get(connection, obj2, propertyName);
        }
    }

    public static string GetEnumPresentation(Enum value, V8Metadata metadata)
    {
        string name = value.GetType().Name;
        V8TypeInfo info = metadata.TypesInfo[name];
        if ((info != null) && ((info.EnumValues != null) && (info.EnumValues.Length > 0)))
        {
            for (int i = 0; i < info.EnumValues.Length; i++)
            {
                if (info.EnumValues[i].Equals(value))
                {
                    return info.EnumPresentations[i];
                }
            }
        }
        return value.ToString();
    }

    public static ComObject GetObject(V8DbConnection connection, ObjectRef reference)
    {
        if (connection.State == ConnectionState.Closed)
        {
            throw new Exception(GetString("err_nonConnectedInfobase"));
        }
        if (reference.IsEmpty())
        {
            throw new Exception(GetString("err_referenceIsEmpty"));
        }
        using (ComObject obj2 = reference.Reference(connection))
        {
            return new ComObject(Call(obj2, "GetObject", new object[0]));
        }
    }

    public static object GetObject(V8TypeInfo typeInfo, string val)
    {
        if ((typeInfo.Category == V8TypeCategory.SystemEnumeration) || (typeInfo.Category == V8TypeCategory.Enumeration))
        {
            if (val == string.Empty)
            {
                val = "EmptyRef";
            }
            return Enum.Parse(typeInfo.DataType, val);
        }
        object obj2 = Activator.CreateInstance(typeInfo.DataType);
        ((ObjectRef) obj2).UUID = new Guid(val);
        return obj2;
    }

    public static string GetString(string name)
    {
        if (resourceManager == null)
        {
            resourceManager = new ResourceManager("_1C.V8.Data.strings", Assembly.GetExecutingAssembly());
        }
        return resourceManager.GetString(name);
    }

    public static string GetString(string name, string lang)
    {
        if (resourceManager == null)
        {
            resourceManager = new ResourceManager("_1C.V8.Data.strings", Assembly.GetExecutingAssembly());
        }
        return resourceManager.GetString(name, new CultureInfo(lang));
    }

    public static void Put(ComObject target, string propertyName, object propertyValue)
    {
        target.comObject.GetType().InvokeMember(propertyName, BindingFlags.PutDispProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, target.comObject, new object[] { propertyValue });
    }

    public static void Put(V8DbConnection connection, ComObject target, string propertyName, object propertyValue)
    {
        object obj2 = ConvertValueNetToV8(propertyValue, connection);
        try
        {
            Put(target, propertyName, obj2);
        }
        finally
        {
            if (((obj2 != null) && !Marshal.IsComObject(propertyValue)) && (!(propertyValue is ComObject) && Marshal.IsComObject(obj2)))
            {
                ReleaseComObject(obj2);
            }
        }
    }

    public static void RegisterComObject(object obj)
    {
        TraceComObject.RegisterComObject(obj);
    }

    public static void ReleaseComObject(object obj)
    {
        TraceComObject.ReleaseComObject(obj);
    }

    public static string ToInvariantString(object o)
    {
        if ((o == null) || (o == Undefined.Value))
        {
            return string.Empty;
        }
        if (o is Enum)
        {
            return o.ToString();
        }
        if (o is ObjectRef)
        {
            return ((ObjectRef) o).ToInvariantString();
        }
        if (o is DateTime)
        {
            DateTime time = (DateTime) o;
            return time.ToString("G");
        }
        return TypeDescriptor.GetConverter(o).ConvertToString(o);
    }

    public static string TypeNameFrom1CValue(object value, V8DbConnection connection)
    {
        if (value == null)
        {
            return string.Empty;
        }
        string xmlTypeName = connection.XMLTypeNameOfValue(value);
        if (xmlTypeName.Length == 0)
        {
            return string.Empty;
        }
        return V8Metadata.GetMetadata(connection.IBAlias).TypesInfo.GetByXmlName(xmlTypeName).Name;
    }

    public static string TypeNameFromValue(object value)
    {
        string name = string.Empty;
        if ((value != Undefined.Value) && (value != null))
        {
            if (value is ObjectRef)
            {
                return value.GetType().Name;
            }
            if (((value is decimal) || (value is int)) || (value is double))
            {
                return "decimal";
            }
            if (value is string)
            {
                return "string";
            }
            if (value is DateTime)
            {
                return "dateTime";
            }
            if (value is bool)
            {
                return "boolean";
            }
            if (value is DBNull)
            {
                return "Null";
            }
            if (value is TypeDescription)
            {
                return "TypeDescription";
            }
            if (value is Enum)
            {
                name = value.GetType().Name;
            }
        }
        return name;
    }

    // Nested Types
    public class TraceComObject
    {
        // Fields
        private static ArrayList m_traceComObjectArrayObj = new ArrayList();
        private static bool m_traceComObjectEnabled = false;
        private static StringCollection m_traceComObjectStack = new StringCollection();

        // Methods
        private TraceComObject()
        {
        }

        public static string GetStackTrace(int index)
        {
            if ((index < 0) || (index >= m_traceComObjectStack.Count))
            {
                throw new ArgumentOutOfRangeException();
            }
            return m_traceComObjectStack[index];
        }

        public static void RegisterComObject(object obj)
        {
            if ((m_traceComObjectEnabled && (obj != null)) && Marshal.IsComObject(obj))
            {
                string str = string.Empty;
                StackTrace trace = new StackTrace(true);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    str = str + trace.GetFrame(i).ToString();
                }
                m_traceComObjectArrayObj.Add(obj);
                m_traceComObjectStack.Add(str);
            }
        }

        public static void ReleaseComObject(object obj)
        {
            if ((obj != null) && Marshal.IsComObject(obj))
            {
                if (m_traceComObjectEnabled)
                {
                    for (int i = m_traceComObjectArrayObj.Count - 1; i >= 0; i--)
                    {
                        if (m_traceComObjectArrayObj[i] == obj)
                        {
                            m_traceComObjectArrayObj.RemoveAt(i);
                            m_traceComObjectStack.RemoveAt(i);
                            break;
                        }
                    }
                }
                Marshal.ReleaseComObject(obj);
            }
        }

        public static void Start()
        {
            m_traceComObjectEnabled = true;
            m_traceComObjectArrayObj.Clear();
            m_traceComObjectStack.Clear();
        }

        public static void Stop()
        {
            m_traceComObjectEnabled = false;
            m_traceComObjectArrayObj.Clear();
            m_traceComObjectStack.Clear();
        }

        // Properties
        public static int Count
        {
            get
            {
                return m_traceComObjectStack.Count;
            }
        }

        public static bool IsStarted
        {
            get
            {
                return m_traceComObjectEnabled;
            }
        }
    }
}

/*

*/

public class TraceComObject
{
    // Fields
    private static ArrayList m_traceComObjectArrayObj = new ArrayList();
    private static bool m_traceComObjectEnabled = false;
    private static StringCollection m_traceComObjectStack = new StringCollection();

    // Methods
    private TraceComObject()
    {
    }

    public static string GetStackTrace(int index)
    {
        if ((index < 0) || (index >= m_traceComObjectStack.Count))
        {
            throw new ArgumentOutOfRangeException();
        }
        return m_traceComObjectStack[index];
    }

    public static void RegisterComObject(object obj)
    {
        if ((m_traceComObjectEnabled && (obj != null)) && Marshal.IsComObject(obj))
        {
            string str = string.Empty;
            StackTrace trace = new StackTrace(true);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                str = str + trace.GetFrame(i).ToString();
            }
            m_traceComObjectArrayObj.Add(obj);
            m_traceComObjectStack.Add(str);
        }
    }

    public static void ReleaseComObject(object obj)
    {
        if ((obj != null) && Marshal.IsComObject(obj))
        {
            if (m_traceComObjectEnabled)
            {
                for (int i = m_traceComObjectArrayObj.Count - 1; i >= 0; i--)
                {
                    if (m_traceComObjectArrayObj[i] == obj)
                    {
                        m_traceComObjectArrayObj.RemoveAt(i);
                        m_traceComObjectStack.RemoveAt(i);
                        break;
                    }
                }
            }
            Marshal.ReleaseComObject(obj);
        }
    }

    public static void Start()
    {
        m_traceComObjectEnabled = true;
        m_traceComObjectArrayObj.Clear();
        m_traceComObjectStack.Clear();
    }

    public static void Stop()
    {
        m_traceComObjectEnabled = false;
        m_traceComObjectArrayObj.Clear();
        m_traceComObjectStack.Clear();
    }

    // Properties
    public static int Count
    {
        get
        {
            return m_traceComObjectStack.Count;
        }
    }

    public static bool IsStarted
    {
        get
        {
            return m_traceComObjectEnabled;
        }
    }
}

 
/*
*/

[Serializable, StructLayout(LayoutKind.Sequential), ComVisible(true)]
public struct ParameterModifier
{
    private bool[] _byRef;
    public ParameterModifier(int parameterCount)
    {
        if (parameterCount <= 0)
        {
            throw new ArgumentException(Environment.GetResourceString("Arg_ParmArraySize"));
        }
        this._byRef = new bool[parameterCount];
    }

    internal bool[] IsByRefArray
    {
        get
        {
            return this._byRef;
        }
    }
    public bool this[int index]
    {
        get
        {
            return this._byRef[index];
        }
        set
        {
            this._byRef[index] = value;
        }
    }
}

 
/*
*/
 

