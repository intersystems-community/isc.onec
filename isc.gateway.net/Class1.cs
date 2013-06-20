using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WcfServiceLib.Test.ServiceReference;
using System.Xml.Serialization;
using System.IO;


namespace WcfServiceLibTest3
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void FindPersonTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();

            try
            {
                var req = new WcfServiceLib.Test.ServiceReference.FindPersonRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds()

                    // , Lastname = "Егоров", Firstname="Максим", Phone = "9162165634"

                    //Firstname = "Владимир",
                        //Middlename = "",
                        //Lastname = "Медведев",
                        //Firstname = "",
                        //Lastname = ""
                    ,
                    UnistreamCardNumber = "004378890" // "007000014"// "004378890"  // 
                    //, Phone="4266425"
                };

                var findPersonResponse = unistream.FindPerson(req);
                CheckFault(findPersonResponse);


                findPersonResponse.Persons.ToList().ForEach(person
                    =>
                    TestContext.WriteLine(@" {0} {1} {2} {3} / {4} {5} {6}", person.ID, person.FirstName, person.MiddleName, person.LastName, person.FirstNameLat, person.MiddleNameLat, person.LastNameLat)
                    );

                TestContext.WriteLine(@"\r\n\r\n{0}", Serialize(findPersonResponse.Persons));
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void CreatePersonTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            try
            {
                var req = new WcfServiceLib.Test.ServiceReference.CreatePersonRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    Person = new Person()
                    {
                        BirthDate = DateTime.Now.Date.AddYears(-50).Clarify(),
                        FirstName = "Джордж160",
                        FirstNameLat = "",
                        MiddleName = "Уокер160",
                        MiddleNameLat = "",
                        LastName = "Буш160",
                        LastNameLat = "",
                        Phones = new Phone[]{new Phone() 
                                                   { CountryID=18, AreaCode="919", Ext = "", Number="234970"}
                                                   },
                        Address = new PersonAddress()
                        {
                            Building = "11",
                            City = "Нью-Хейвен-17",
                            CountryID = 18,
                            Flat = "018",
                            House = "11",
                            PostalCode = "11112",
                            Street = "Красная площадь899"
                            ,
                        },
                        //Documents = new Document[]
                        //                {
                        //                    new Document()
                        //                        {

                        //                            TypeID = 35,
                        //                            Number = "1191722",
                        //                            Series = "11",
                        //                            Issuer = "11Д99882",
                        //                            IssueDate =
                        //                                DateTime.Now.AddDays(-1000).Clarify(),
                        //                            ExpiryDate =
                        //                                DateTime.Now.AddDays(1000).Clarify(),
                        //                            IssuerCode = "1199"
                        //                        },
                        //                    new Document()
                        //                        {

                        //                            TypeID = 1,
                        //                            Number = "111982211",
                        //                            Series = "3311",
                        //                            Issuer = "МВ2",
                        //                            IssueDate =
                        //                                DateTime.Now.AddDays(-500).Clarify(),
                        //                            IssuerCode = "22291"
                        //                        },
                        //                },


                        //    Residentships= new Residentship[]
                        //               {
                        //                   new Residentship()
                        //                       {
                        //                           CountryID = 18,
                        //                           IsResident=true,
                        //                       }, 
                        //                   //new Residentship()
                        //                   //    {
                        //                   //        CountryID = 6,
                        //                   //        IsResident=false,


                        //                   //    }, 

                        //               }
                        //               ,
                        UnistreamCardNumber = "001000160"

                    }
                };


                var resp = unistream.CreatePerson(req);

                CheckFault(resp);

                TestContext.WriteLine("resp.Person.ID=" + resp.Person.ID);
                TestContext.WriteLine("{0}", Serialize(resp.Person));

            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void GetCountriesTestWcf2_20()
        {
            for (int i = 0; i < 20; i++)
            {
                TestContext.WriteLine("");
                TestContext.WriteLine("");
                TestContext.WriteLine("");
                TestContext.WriteLine("");
                TestContext.WriteLine("i={0}", i);

                WebServiceClient unistream = new WebServiceClient();

                var req = new GetCountriesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds()
                };

                var resp = unistream.GetCountries(req);

                CheckFault(resp);

                TestContext.WriteLine("c={0}", resp.Countries.Count());

            }

        }


        [TestMethod]
        public void GetCountriesTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            try
            {

                var req = new GetCountriesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds()
                };

                var resp = unistream.GetCountries(req);

                CheckFault(resp);

                TestContext.WriteLine
                    (
                    resp.Countries.Select(
                        con =>
                        con.Name.FirstOrDefault().Text + ", " + con.ID + ", " + con.Digital + ", " + con.Latin3 + ", " +
                        con.UpdateCount + ", " + con.PhoneCode).Aggregate(
                        (all, next) => all + Environment.NewLine + next)
                    );
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void GetBankByIDTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            try
            {
                TestContext.WriteLine("{0}", unistream.Endpoint.Address.Uri);

                var req = new GetBankByIDRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    ID = 26 //166817
                };

                var resp = unistream.GetBankByID(req);

                CheckFault(resp);

                TestContext.WriteLine(resp.Bank.Name.First().Text);

                TestContext.WriteLine(
                    "bank.ID={0}, PaysTransfer={1}, SendsTransfer={2}, UpdateCount={3}, Type={4}, bank.Name={5}, bank.Address.Street={6}, bank.Phone={7}, bank.ParentID={8}, Bank.Address.RegionID={9}",
                    resp.Bank.ID,
                    resp.Bank.Flags.PaysTransfer,
                    resp.Bank.Flags.SendsTransfer,
                    resp.Bank.UpdateCount,
                    resp.Bank.Type,
                    resp.Bank.Name != null
                        ? resp.Bank.Name.Select(n => n.Text).Aggregate((all, next) => all + "/" + next)
                        : "",
                    resp.Bank.Address == null ? "" : resp.Bank.Address.Street,
                    resp.Bank.Phone == null ? "" : resp.Bank.Phone.AreaCode + resp.Bank.Phone.Number,
                    resp.Bank.ParentID,
                    resp.Bank.Address == null ? -1 : resp.Bank.Address.RegionID
                    );




            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }



        [TestMethod]
        public void GetNoticesChangesTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            try
            {
                TestContext.WriteLine("{0}", unistream.Endpoint.Address.Uri);

                var req = new GetNoticesChangesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    UpdateCount = long.MaxValue
                };

                var resp = unistream.GetNoticesChanges(req);

                CheckFault(resp);

                TestContext.WriteLine("len={0}", resp.Notices.Length);

                TestContext.WriteLine("xml=", Serialize(resp));


            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }



        private WcfServiceLib.Test.ServiceReference.AuthenticationHeader GetTestCreds()
        {
            return new WcfServiceLib.Test.ServiceReference.AuthenticationHeader()
            {
                AppKey = "1wwteyFGFew624",  // valid for test environment only
                Username = "yourusername",  // ask unistream
                Password = "somepassword",


            };
        }




        [TestMethod]
        public void GetDocumentTypesTestWcf()
        {
            WebServiceClient unistream = new WebServiceClient();
            TestContext.WriteLine("unistream.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);
            try
            {
                var req = new GetDocumentTypeChangesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    UpdateCount = 0
                };

                var resp = unistream.GetDocumentTypeChanges(req);

                TestContext.WriteLine
                    (
                    resp.DocumentTypes
                        .Select(con =>
                                con.ID + "; " +
                                con.Name.Select(n => n.Text).Aggregate((l1, l2) => l1 + "," + l2) + "; " +
                                con.UpdateCount)
                        .Aggregate((all, next) => all + Environment.NewLine + next)
                    );
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }



        [TestMethod]
        public void RndPauseTestWcf2()
        {
            var i = Convert.ToInt32(new Random().NextDouble() * 1000);
            Thread.Sleep(i);
            TestContext.WriteLine("i={0}", i);
        }

        [TestMethod]
        public void InsertPayoutTransferTestWcf2_cicle()
        {
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    InsertPayoutTransferTestWcf2();
                    TestContext.WriteLine("{0} {1} ok", DateTime.Now.ToLongTimeString(), i);
                }
                catch (Exception e)
                {

                    TestContext.WriteLine("{0} {1} err ", DateTime.Now.ToLongTimeString(), i);
                }
            }
        }


        [TestMethod]
        public void InsertPayoutTransferTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            try
            {

                TestContext.WriteLine("unistream.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);

                // идентификатор пункта(конкретного пуннкта!) - отправителя перевода
                var SenderBankID = 166817;
                // 166817 - касса 159 КБ Юнистрим (Moscow, Kozhevnicheskaya st. b. 7/1)

                // идентификатор пункта - получателя перевода
                var ReceiverBankID = 166817;// 118633; // 133-[Украина], 227- молдова, 9017 италия , 184103 - ощад_профикс_тест

                // идентификатор пункта - фактического получателя перевода
                var ActualReceiverBankID = 166817; // 87237-касса 133 кб юнистрим



                // валюта перевода - 1=рубль
                var currencyID = 1;
                var rnd = new Random();

                // сумма перевода
                var amount = rnd.Next(0, 100) + rnd.Next(0, 99) / 100.0;
                //amount = 30;

                var promocode = "";

                // собственный идентификатор перевода в системе отправителя
                var sourceID = rnd.Next(1, 100000); //95547; // 

                TestContext.WriteLine("sourceID={0}", sourceID);

                InsertTransferRequestMessage req = new InsertTransferRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds()
                };

                Transfer transfer = new Transfer();

                transfer.Type = TransferType.Remittance;
                transfer.SourceID = sourceID;
                transfer.SentDate = DateTime.Now.Clarify();
                transfer.PromoCode = promocode;
                TestContext.WriteLine("amount={0}", amount);

                transfer.Amounts = new Amount[]
                                       {
                                           new Amount() {CurrencyID = currencyID, Sum = amount, Type = AmountType.Main},
                                           new Amount()
                                               {CurrencyID = currencyID, Sum = amount, Type = AmountType.ActualPaid},
                                           new Amount()
                                               {
                                                   CurrencyID = currencyID,
                                                   Sum = 0,
                                                   Type = AmountType.PrimaryPaidComission
                                               }
                                       };

                transfer.ControlNumber = "";

                // пункты системы юнистрим, которые обслуживают данный перевод
                transfer.Participators = new Participator[]
                                             {
                                                 new Participator()
                                                     {
                                                         ID = SenderBankID,
                                                         Role = ParticipatorRole.SenderPOS
                                                     },
                                                 new Participator()
                                                     {
                                                         ID = ReceiverBankID,
                                                         Role = ParticipatorRole.ExpectedReceiverPOS
                                                     }
                                             };

                // клиенты
                transfer.Consumers = new Consumer[]
                                         {
                                           

                                             new Consumer()
                                                 {
                                                     Person = new Person()
                                                                  {

                                                                      FirstName = "Джордж",
                                                                      MiddleName = "Уокер",
                                                                      LastName = "Буш",
                                                                      ID = 8451951
                                                                  },


                        


                                                     Role = ConsumerRole.ExpectedReceiver
                                                 },
                                             new Consumer()
                                                 {
                                                     Person = new Person()
                                                                  {
                                                             

                                                                       FirstName = "Джордж160",
                                                                       MiddleName = "Уокер160",
                                                                       LastName = "Буш160",
                                                                       ID = 8475148
                                                                 

                                                                  },


                                                     Role = ConsumerRole.Sender
                                                 }
                                         };

                // вычислим комиссии
                var prepareResult =
                    unistream.PrepareTransfer(new PrepareTransferRequestMessage() { AuthenticationHeader = GetTestCreds(), Transfer = transfer });

                CheckFault(prepareResult);

                var services = prepareResult.Transfer.Services.ToList();

                TestContext.WriteLine("Transfer.Comment={0}", prepareResult.Transfer.Comment);
                transfer.Comment = prepareResult.Transfer.Comment;
                TestContext.WriteLine("prepareResult.Transfer.ControlNumber={0}", prepareResult.Transfer.ControlNumber);
                transfer.ControlNumber = prepareResult.Transfer.ControlNumber;

                // выведем комиссии
                services.ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID));
                services.ForEach(
                    s => s.Response = s.Mode == ServiceMode.Required ? Response.Accepted : Response.Rejected);


                transfer.Services = services.ToArray();


                transfer.Amounts.Single(a => a.Type == AmountType.PrimaryPaidComission).Sum
                    = services
                        .Where(s => s.ServiceID >= 1 && s.ServiceID <= 3)
                        .Sum(s => s.Fee);


                transfer.CashierUserAction = new UserActionInfo()
                {
                    ActionLocalDateTime = DateTime.Now.Clarify2(),
                    UserID = 1,
                    UserUnistreamCard = "hello1"
                };

                transfer.TellerUserAction = new UserActionInfo()
                {
                    ActionLocalDateTime = DateTime.Now.Clarify2(),
                    UserID = 2,
                    UserUnistreamCard = "hello2"
                };

                req.Transfer = transfer;

                // отправим перевод
                var insertTransferResponse = unistream.InsertTransfer(req);
                CheckFault(insertTransferResponse);


                TestContext.WriteLine(
                    "insertTransferResponse: Transfer.ID={0}, resp.Transfer.ControlNumber={1}, resp.Transfer.Status={2}, SourceID={3}",
                    insertTransferResponse.Transfer.ID, insertTransferResponse.Transfer.ControlNumber,
                    insertTransferResponse.Transfer.Status, insertTransferResponse.Transfer.SourceID);

                return;
                //throw new ApplicationException("только отправка перевода");

                var alterControl = insertTransferResponse.Transfer.ControlNumber;

                // получим перевод по sourceID
                var getResponse =
                    unistream.GetTransferBySourceID(new GetTransferBySourceIDRequestMessage() { AuthenticationHeader = GetTestCreds(), SourceID = sourceID });
                CheckFault(getResponse);

                TestContext.WriteLine(
                    @"getResponse.Transfer.ID={0}, getResponse.Transfer.SourceID={1}, getResponse.Transfer.ControlNumber={2}",
                    getResponse.Transfer.ID, getResponse.Transfer.SourceID, getResponse.Transfer.ControlNumber);
                Assert.AreEqual(insertTransferResponse.Transfer.ID, getResponse.Transfer.ID);

                TestContext.WriteLine("Комиссии :");
                // выведем комиссии
                getResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}",
                        s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));


                //return;

                //// вставим уведомление 
                //var notice = new Notice();
                //notice.Type = NoticeType.ChangeReceiver;
                //notice.Transfer = getResponse.Transfer;

                //notice.Consumers = new Consumer[]
                //                       {
                //                           new Consumer()
                //                               {
                //                                   Person = new Person()
                //                                                {
                //                                                    BirthDate = DateTime.MinValue,
                //                                                   FirstName = "Джордж",
                //                                                   MiddleName = "Уокер",
                //                                                   LastName = "Буш",
                //                                                   ID = 8451951
                //                                                },
                //                                   Role = ConsumerRole.ExpectedReceiver
                //                               }
                //                       };

                //notice.Participators = new Participator[]
                //                                               {
                //                                                   //new Participator()
                //                                                   //    {
                //                                                   //        ID = 128087,
                //                                                   //        Role = ParticipatorRole.ExpectedReceiverPOS
                //                                                   //    }
                //                                               };

                //var prepNoticeRequest = new PrepareNoticeRequestMessage();
                //prepNoticeRequest.AuthenticationHeader = GetTestCreds();
                //prepNoticeRequest.Notice = notice;
                //var prepNoticeResponse = unistream.PrepareNotice(prepNoticeRequest);
                //CheckFault(prepNoticeResponse);

                //var insertNoticeRequest = new InsertNoticeRequestMessage();
                //insertNoticeRequest.AuthenticationHeader = GetTestCreds();
                //insertNoticeRequest.Notice = notice;

                //var insertNoticeResponse = unistream.InsertNotice(insertNoticeRequest);

                //CheckFault(insertNoticeResponse);

                //TestContext.WriteLine(
                //    "insertNoticeResponse: Notice.ID={0}, Notice.Status={1}, Notice.Transfer.Status={2}",
                //    insertNoticeResponse.Notice.ID, insertNoticeResponse.Notice.Status,
                //    insertNoticeResponse.Notice.Transfer.Status);
                //TestContext.WriteLine("insertNoticeResponse Transfer.NoticeList: {0}",
                //                      Serialize(insertNoticeResponse.Notice.Transfer.NoticeList));

                ////return;
                //getResponse = unistream.GetTransferBySourceID(new GetTransferBySourceIDRequestMessage() { AuthenticationHeader = GetTestCreds(), SourceID = sourceID });
                //CheckFault(getResponse);
                //TestContext.WriteLine("getResponse.Transfer.NoticeList: {0}", Serialize(getResponse.Transfer.NoticeList));

                //var getResponse2 =
                //    unistream.GetTransferByID(new GetTransferByIDRequestMessage()
                //                                  {
                //                                      AuthenticationHeader = GetTestCreds(),
                //                                      TransferID = getResponse.Transfer.ID
                //                                  });
                //CheckFault(getResponse2);
                //TestContext.WriteLine("getResponse2.Transfer.NoticeList: {0}",
                //                      Serialize(getResponse2.Transfer.NoticeList));



                //// согласуем уведомление
                //var approveNoticeResponse = unistream.ApproveNotice(new ApproveNoticeRequestMessage() { AuthenticationHeader = GetTestCreds(), Notice = insertNoticeResponse.Notice });
                //CheckFault(approveNoticeResponse);
                //TestContext.WriteLine("approveNoticeResponse.Notice.ID={0}, approveNoticeResponse.Notice.Status={1} approveNoticeResponse.Notice.Transfer.Status={2}",
                //    approveNoticeResponse.Notice.ID, approveNoticeResponse.Notice.Status, approveNoticeResponse.Notice.Transfer.Status);

                //// вернём перевод
                //if (approveNoticeResponse.Notice.Transfer.Status == TransferStatus.Cancelled)
                //{
                //    var returningTransfer = approveNoticeResponse.Notice.Transfer;
                //    // добавим фактического получателя
                //    var consumers2 = returningTransfer.Consumers.ToList();
                //    consumers2.Add(
                //                new Consumer()
                //                {
                //                    Person = new Person()
                //                    {
                //                        BirthDate = DateTime.MinValue,
                //                        FirstName = "Джордж",
                //                        MiddleName = "Уокер",
                //                        LastName = "Буш",
                //                        ID = 8451951
                //                    },
                //                    Role = ConsumerRole.ActualReceiver
                //                }
                //        );

                //    returningTransfer.Consumers = consumers2.ToArray();


                //    // добавим фактический пункт выплаты
                //    var participators2 = returningTransfer.Participators.ToList();
                //    participators2.Add(new Participator { ID = ActualReceiverBankID, Role = ParticipatorRole.ActualReceiverPOS });
                //    returningTransfer.Participators = participators2.ToArray();

                //    var returnTransferResponse = unistream.ReturnTransfer(
                //        new ReturnTransferRequestMessage()
                //            {
                //                AuthenticationHeader = GetTestCreds(),
                //                Transfer = returningTransfer
                //            });

                //    CheckFault(returnTransferResponse);
                //    TestContext.WriteLine(
                //        @"returnTransferResponse.Transfer.ID={0}, Transfer.SentDate={1}, Transfer.Status={2}",
                //        returnTransferResponse.Transfer.ID, returnTransferResponse.Transfer.SentDate, returnTransferResponse.Transfer.Status);
                //    TestContext.WriteLine("юнистрим разрешил возврат перевода");
                //    return;
                //}


                // найдём перевод для выдачи
                //var findResponse = getResponse;                

                var findResponse = unistream.FindTransfer(new FindTransferRequestMessage
                {
                    AuthenticationHeader = GetTestCreds(),
                    ControlNumber = alterControl,
                    CurrencyID = currencyID,
                    Sum = amount,
                    BankID = ActualReceiverBankID
                });

                CheckFault(findResponse);

                if (findResponse.Transfer == null)
                    throw new ApplicationException("По указаннным критериям перевод доступный для выдачи из текущего пункта не найден");

                TestContext.WriteLine(
                    @"findResponse.Transfer.ID={0}, findResponse.Transfer.SentDate={1}, findResponse.Transfer.Status={2}",
                    findResponse.Transfer.ID, findResponse.Transfer.SentDate, findResponse.Transfer.Status);

                TestContext.WriteLine(
                    @"findResponse.Transfer.NoticeList={0}", Serialize(findResponse.Transfer.NoticeList));

                // выведем комиссии
                TestContext.WriteLine("комиссии в найденном переводе:");
                findResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID));

                // выведем суммы
                TestContext.WriteLine("суммы в найденном переводе:");
                findResponse.Transfer.Amounts.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"CurrencyID={0}, Sum={1}, Type={2}",
                        s.CurrencyID, s.Sum, s.Type));



                // добавим фактического получателя
                var consumers = findResponse.Transfer.Consumers.ToList();
                consumers.Add(
                    new Consumer()
                    {
                        Person = new Person()
                        {
                            BirthDate = DateTime.MinValue,
                            //FirstName = "Ярослав",
                            //MiddleName = "Антонович",
                            //LastName = "Петров",
                            //ID = 1226331
                            FirstName = "Джордж",
                            MiddleName = "Уокер",
                            LastName = "Буш",
                            ID = 8451951
                        },
                        Role = ConsumerRole.ActualReceiver
                    }
                    );

                findResponse.Transfer.Consumers = consumers.ToArray();


                // добавим фактический пункт выплаты
                var participators = findResponse.Transfer.Participators.ToList();
                participators.Add(new Participator { ID = ActualReceiverBankID, Role = ParticipatorRole.ActualReceiverPOS });

                findResponse.Transfer.Participators = participators.ToArray();


                // добавим фактически выплаченную сумму (если отличается от Main)
                var amounts = findResponse.Transfer.Amounts.ToList();
                amounts.Add(new Amount { CurrencyID = 1, Sum = 500, Type = AmountType.ActualPaidout });

                findResponse.Transfer.Amounts = amounts.ToArray();

                // выплатим перевод 
                var payoutResponse = unistream.PayoutTransfer(new PayoutTransferRequestMessage
                {
                    AuthenticationHeader = GetTestCreds(),
                    Transfer = findResponse.Transfer
                });

                CheckFault(payoutResponse);

                TestContext.WriteLine(
                    "Юнистрим разрешил выплату перевода. payoutResponse.Transfer.ID={0}, payoutResponse.Transfer.ControlNumber={1}, payoutResponse.Transfer.Status={2}, PayoutDate={3}",
                    payoutResponse.Transfer.ID, payoutResponse.Transfer.ControlNumber, payoutResponse.Transfer.Status,
                    payoutResponse.Transfer.PayoutDate);

                // выведем комиссии
                TestContext.WriteLine("");
                TestContext.WriteLine("комиссии в выплаченном переводе:");
                payoutResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID));

                // выведем суммы
                TestContext.WriteLine("суммы в выплаченном переводе:");
                payoutResponse.Transfer.Amounts.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"CurrencyID={0}, Sum={1}, Type={2}",
                        s.CurrencyID, s.Sum, s.Type));




                TestContext.WriteLine("участники в выплаченном переводе:");
                payoutResponse.Transfer.Participators.ToList().ForEach(
                    p =>
                    TestContext.WriteLine(
                        @"p.ID={0}, p.Role={1}",
                        p.ID, p.Role));




            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        private static string Serialize(object obj)
        {
            if (obj == null)
                return "null";

            XmlSerializer x = new XmlSerializer(obj.GetType());
            byte[] b;

            using (MemoryStream ms = new MemoryStream())
            {
                x.Serialize(ms, obj);
                b = ms.ToArray();
            }

            var str = Encoding.UTF8.GetString(b);

            return str;
        }
        [TestMethod]
        public void FindPayoutTransferTest2()
        {
            var unistream = new WebServiceClient();

            var ActualReceiverBankID = 87237; // 87237-касса 133 кб юнистрим

            try
            {
                var findResponse = unistream.FindTransfer(new FindTransferRequestMessage
                {
                    AuthenticationHeader = GetTestCreds(),
                    ControlNumber = "100871703557",
                    CurrencyID = 1,
                    Sum = 54,
                    BankID = ActualReceiverBankID
                });

                CheckFault(findResponse);

                if (findResponse.Transfer == null)
                    throw new ApplicationException("По указаннным критериям перевод доступный для выдачи из текущего пункта не найден");



                TestContext.WriteLine("Комиссии найденного:");
                // выведем комиссии
                findResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));

                TestContext.WriteLine(
                    @"findResponse.Transfer.ID={0}, findResponse.Transfer.SentDate={1}, findResponse.Transfer.Status={2}",
                    findResponse.Transfer.ID, findResponse.Transfer.SentDate, findResponse.Transfer.Status);


                TestContext.WriteLine("Клиенты найденного:");
                findResponse.Transfer.Consumers.ToList().ForEach(
                    c =>
                    TestContext.WriteLine(
                        @"c.Role={0}, c.Person.FirstName={1}, c.Person.MiddleName={2}, c.Person.LastName={3}, c.Person.UnistreamCardNumber={4}",
                        c.Role, c.Person.FirstName, c.Person.MiddleName, c.Person.LastName, c.Person.UnistreamCardNumber));


                //return;

                // добавим фактического получателя
                var consumers = findResponse.Transfer.Consumers.ToList();
                consumers.Add(
                    new Consumer()
                    {
                        Person = new Person()
                        {
                            BirthDate = DateTime.MinValue,
                            FirstName = "Ярослав",
                            MiddleName = "Антонович",
                            LastName = "Петров",
                            FirstNameLat = "Yaroslav",
                            MiddleNameLat = "Antonovich",
                            LastNameLat = "Petrov",
                            ID = 1226331
                        },
                        Role = ConsumerRole.ActualReceiver
                    }
                    );

                findResponse.Transfer.Consumers = consumers.ToArray();


                // добавим фактический пункт выплаты
                var participators = findResponse.Transfer.Participators.ToList();
                participators.Add(new Participator { ID = ActualReceiverBankID, Role = ParticipatorRole.ActualReceiverPOS });

                findResponse.Transfer.Participators = participators.ToArray();

                // выплатим перевод получателю
                var payoutResponse = unistream.PayoutTransfer(new PayoutTransferRequestMessage
                {
                    AuthenticationHeader = GetTestCreds(),
                    Transfer = findResponse.Transfer
                });

                CheckFault(payoutResponse);

                TestContext.WriteLine(
                    "Юнистрим разрешил выплату перевода. payoutResponse.Transfer.ID={0}, payoutResponse.Transfer.ControlNumber={1}, payoutResponse.Transfer.Status={2}, PayoutDate={3}",
                    payoutResponse.Transfer.ID, payoutResponse.Transfer.ControlNumber, payoutResponse.Transfer.Status,
                    payoutResponse.Transfer.PayoutDate);


                TestContext.WriteLine("Комиссии выплаченного:");
                // выведем комиссии
                payoutResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));

                TestContext.WriteLine("Клиенты выплаченного:");
                payoutResponse.Transfer.Consumers.ToList().ForEach(
                    c =>
                    TestContext.WriteLine(
                        @"c.Role={0}, c.Person.FirstName={1}, c.Person.MiddleName={2}, c.Person.LastName={3}, c.Person.UnistreamCardNumber={4}",
                        c.Role, c.Person.FirstName, c.Person.MiddleName, c.Person.LastName, c.Person.UnistreamCardNumber));
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void FindTransferApproveNoticeTest2()
        {
            var unistream = new WebServiceClient();

            var ActualReceiverBankID = 87237; // 87237-касса 133 кб юнистрим

            try
            {
                var findResponse = unistream.FindTransfer(new FindTransferRequestMessage
                {
                    AuthenticationHeader = GetTestCreds(),
                    ControlNumber = "216766510772",
                    CurrencyID = 3,
                    Sum = 845,
                    BankID = ActualReceiverBankID
                });

                CheckFault(findResponse);

                if (findResponse.Transfer == null)
                    throw new ApplicationException("По указаннным критериям перевод доступный для выдачи из текущего пункта не найден");



                TestContext.WriteLine("Комиссии найденного:");
                // выведем комиссии
                findResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));

                TestContext.WriteLine(
                    @"findResponse.Transfer.ID={0}, findResponse.Transfer.SentDate={1}, findResponse.Transfer.Status={2}",
                    findResponse.Transfer.ID, findResponse.Transfer.SentDate, findResponse.Transfer.Status);


                TestContext.WriteLine("Клиенты найденного:");
                findResponse.Transfer.Consumers.ToList().ForEach(
                    c =>
                    TestContext.WriteLine(
                        @"c.Role={0}, c.Person.FirstName={1}, c.Person.MiddleName={2}, c.Person.LastName={3}, c.Person.UnistreamCardNumber={4}",
                        c.Role, c.Person.FirstName, c.Person.MiddleName, c.Person.LastName, c.Person.UnistreamCardNumber));


                //return;

                // добавим фактического получателя

                TestContext.WriteLine("findResponse.Transfer.NoticeList={0}", Serialize(findResponse.Transfer.NoticeList));

                var noticeLink = findResponse.Transfer.NoticeList.Single();

                var getNoticeByIDResp = unistream.GetNoticeByID(new GetNoticeByIDRequestMessage() { ID = noticeLink.NoticeID, AuthenticationHeader = GetTestCreds() });
                CheckFault(getNoticeByIDResp);

                var approveNoticeResp = unistream.ApproveNotice(new ApproveNoticeRequestMessage() { Notice = getNoticeByIDResp.Notice, AuthenticationHeader = GetTestCreds() });
                CheckFault(approveNoticeResp);

                TestContext.WriteLine("approveNoticeResp={0}", Serialize(approveNoticeResp));
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }




        [TestMethod]
        public void GetTransferByIDWcf2()
        {


            WebServiceClient unistream = new WebServiceClient();
            TestContext.WriteLine("client.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);

            try
            {
                var authenticationHeader = GetTestCreds();
                var req = new GetTransferByIDRequestMessage()
                {
                    AuthenticationHeader = authenticationHeader,
                    TransferID = 53011
                };

                var findResponse = unistream.GetTransferByID(req);
                CheckFault(findResponse);

                TestContext.WriteLine(
                     @"Transfer.ID={0}, Transfer.SentDate={1}, Transfer.Status={2}",
                    findResponse.Transfer.ID, findResponse.Transfer.SentDate, findResponse.Transfer.Status);

                TestContext.WriteLine("Комиссии найденного:");
                // выведем комиссии
                findResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));

                TestContext.WriteLine(@"

TransferXml={0}", Serialize(findResponse.Transfer));

            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }



        [TestMethod]
        public void GetTransferByIDAndInsertNoticeWcf2()
        {


            WebServiceClient unistream = new WebServiceClient();
            TestContext.WriteLine("client.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);

            try
            {
                var authenticationHeader = GetTestCreds();
                var req = new GetTransferByIDRequestMessage()
                {
                    AuthenticationHeader = authenticationHeader,
                    TransferID = 50752
                };

                var getResponse = unistream.GetTransferByID(req);
                CheckFault(getResponse);

                TestContext.WriteLine(
                     @"Transfer.ID={0}, Transfer.SentDate={1}, Transfer.Status={2}, ControlNumber={3}",
                    getResponse.Transfer.ID, getResponse.Transfer.SentDate, getResponse.Transfer.Status, getResponse.Transfer.ControlNumber);

                TestContext.WriteLine("Комиссии найденного:");
                // выведем комиссии
                getResponse.Transfer.Services.ToList().ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}, Response={5}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID, s.Response));



                // вставим уведомление 
                var notice = new Notice();
                notice.Type = NoticeType.CancelTransfer;
                notice.Transfer = getResponse.Transfer;

                var prepNoticeRequest = new PrepareNoticeRequestMessage();
                prepNoticeRequest.AuthenticationHeader = GetTestCreds();
                prepNoticeRequest.Notice = notice;
                var prepNoticeResponse = unistream.PrepareNotice(prepNoticeRequest);
                CheckFault(prepNoticeResponse);

                var insertNoticeRequest = new InsertNoticeRequestMessage();
                insertNoticeRequest.AuthenticationHeader = GetTestCreds();
                insertNoticeRequest.Notice = notice;

                //insertNoticeRequest.Notice.Consumers = new Consumer[]
                //                         {
                //                             new Consumer()
                //                                 {
                //                                     //Person = new Person()
                //                                     //             {
                //                                     //                 BirthDate = DateTime.MinValue,
                //                                     //                 FirstName = "Дмитрий",
                //                                     //                 MiddleName = "",
                //                                     //                 LastName = "Самарин",
                //                                     //                 FirstNameLat = "Dmitry",
                //                                     //                 MiddleNameLat = "",
                //                                     //                 LastNameLat = "Samarin",
                //                                     //                 ID = 331518
                //                                     //             },
                //                                     Person = new Person()
                //                                                  {
                //                                                       FirstName = "Владимир",
                //                                                       FirstNameLat = "",
                //                                                       MiddleName = "Владимирович",
                //                                                       MiddleNameLat = "",
                //                                                       LastName = "Медведев",
                //                                                       LastNameLat = "",
                //                                                      ID = 8438707
                //                                                  },
                //                                     Role = ConsumerRole.ExpectedReceiver
                //                                 }
                //                         };

                //insertNoticeRequest.Notice.Participators = new Participator[]
                //                                               {
                //                                                   new Participator()
                //                                                       {
                //                                                           ID = ReceiverBankID,
                //                                                           Role = ParticipatorRole.ExpectedReceiverPOS
                //                                                       }
                //                                               };



                var insertNoticeResponse = unistream.InsertNotice(insertNoticeRequest);

                CheckFault(insertNoticeResponse);

                TestContext.WriteLine("insertNoticeResponse.Notice.ID={0}, insertNoticeResponse.Notice.Status={1}",
                    insertNoticeResponse.Notice.ID, insertNoticeResponse.Notice.Status);



            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void GetTransfersChangesWcf2()
        {


            WebServiceClient client = new WebServiceClient();
            TestContext.WriteLine("client.Endpoint.Address.Uri={0}", client.Endpoint.Address.Uri);

            var authenticationHeader = GetTestCreds();
            var req = new GetTransfersChangesRequestMessage()
            {
                AuthenticationHeader = authenticationHeader,
                UpdateCount = 4243682 // todo GetSavedUpdateCount("transfers")
            };

            var resp = client.GetTransfersChanges(req);
            CheckFault(resp);


            if (resp.Transfers.Length > 0)
            {
                TestContext.WriteLine("new UpdateCount={0}", resp.UpdateCount);

                // todo SaveUpdateCount("transfers", resp.UpdateCount);

                resp.Transfers.ToList().ForEach(
                    transfer =>
                    TestContext.WriteLine("transfer.ID={0}, ControlNumber={1}", transfer.ID, transfer.ControlNumber));
            }
        }



        [TestMethod]
        public void GetBanksChangesTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();



            var req = new GetBanksChangesRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
                UpdateCount = 4449202 // todo: GetSavedUpdCount("bank");
            };

            var resp = unistream.GetBanksChanges(req);

            CheckFault(resp);


            TestContext.WriteLine("response.Banks.Length={0}", resp.Banks.Length);

            foreach (var bank in resp.Banks)
                TestContext.WriteLine(
                    "bank.ID={0}, PaysTransfer={1}, SendsTransfer={2}, UpdateCount={3}, Type={4}, bank.Name={5}, bank.Address.Street={6}, bank.Phone={7}, bank.ParentID={8}",
                    bank.ID,
                    bank.Flags.PaysTransfer,
                    bank.Flags.SendsTransfer,
                    bank.UpdateCount,
                    bank.Type,
                    bank.Name != null
                        ? bank.Name.Select(n => n.Text).Aggregate((all, next) => all + "/" + next)
                        : "",
                    bank.Address == null ? "" : bank.Address.Street,
                    bank.Phone == null ? "" : bank.Phone.AreaCode + bank.Phone.Number,
                    bank.ParentID
                    );

            // todo: SaveUpdCount("bank", resp.Banks.Max(bank => bank.UpdateCount));

            // todo: repeat while resp.Banks.Count > 0 
        }

        [TestMethod]
        public void GetRegionsChangesTestWcf2()
        {
            WebServiceClient unistream = new WebServiceClient();
            TestContext.WriteLine("unistream.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);

            try
            {

                var req = new GetRegionsChangesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    UpdateCount = 1843318
                };

                var resp = unistream.GetRegionsChanges(req);

                CheckFault(resp);

                foreach (var region in resp.Regions)
                    TestContext.WriteLine("region.ID={0}, UpdateCount={1}, CountryID={2}, Name={3}, VirtualBankId={4}, ParentID={5}",
                                          region.ID,
                                          region.UpdateCount,
                                          region.CountryID,
                                          region.Name != null
                                              ? region.Name.Select(n => n.Text).Aggregate(
                                                    (all, next) => all + "/" + next)
                                              : "",
                                          region.VirtualBankId,
                                          region.ParentID


                        );
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }



        [TestMethod]
        public void GetAllBanksTestWcf2()
        {
            int infPortionNo = 0;
            int infTotalCount = 0;


            long maxUpdCount = 0;
            int portionCount;

            var totalVirtualPos = 0;
            var totalBankPos = 0;
            var totalVirtualPosWithAdress = 0;
            var totalBankPosWithOutAdress = 0;
            var totalBankPosWithAdressWithZeroRegion = 0;
            var totalDeletedBanks = 0;

            // todo
            // if (InitialLoading) ClearYourLocalBanksDatabase();
            // else maxUpdCount = GetSavedUpdCount("bank");

            WebServiceClient unistream = new WebServiceClient();
            try
            {
                var req = new GetBanksChangesRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                };



                do
                {
                    req.UpdateCount = maxUpdCount;
                    // Getting portion of data
                    var resp = unistream.GetBanksChanges(req);

                    CheckFault(resp);

                    portionCount = resp.Banks.Length;

                    if (portionCount > 0)
                    {
                        // todo: InsertOrUpdateBanksToYourLocalDatabase(resp.Banks);

                        maxUpdCount = resp.Banks
                            .Max(b => b.UpdateCount);

                        infPortionNo++;
                        infTotalCount += portionCount;

                        totalVirtualPos += resp.Banks.Count(b => b.Type == BankType.Virtual);
                        totalBankPos += resp.Banks.Count(b => b.Type == BankType.Bank);
                        totalVirtualPosWithAdress += resp.Banks.Count(b => b.Type == BankType.Virtual && b.Address != null);
                        totalBankPosWithOutAdress += resp.Banks.Count(b => b.Type == BankType.Bank && b.Address == null);
                        totalBankPosWithAdressWithZeroRegion += resp.Banks.Count(b => b.Type == BankType.Bank && b.Address != null && b.Address.RegionID < 1);
                        totalDeletedBanks += resp.Banks.Count(b => b.Status == ObjectStatus.Deleted);

                        TestContext.WriteLine("Portion {0}, pCount {1}, tCount {2}, MaxUC {3}",
                                              infPortionNo, portionCount, infTotalCount, maxUpdCount);

                        TestContext.WriteLine(
                            @"totalVirtualPos={0}, totalBankPos={1}, totalVirtualPosWithAdress={2}, totalBankPosWithOutAdress={3}, totalBankPosWithAdressWithZeroRegion={4}, totalDeletedBanks={5} ",
                            totalVirtualPos, totalBankPos, totalVirtualPosWithAdress, totalBankPosWithOutAdress,
                            totalBankPosWithAdressWithZeroRegion, totalDeletedBanks);

                    }


                    // todo: consider saving unistream's resourcers. Remove next line when ready
                    if (infPortionNo > 3) break;

                } // while Xml is not empty
                while (portionCount > 0);

                TestContext.WriteLine("done");

                //todo: SaveUpdCount(maxUpdCount, "bank");
                // Save UpdCount here to allow future differential data loading
            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void GetAllRegionsTestWcf2()
        {
            int infPortionNo = 0;
            int infTotalCount = 0;


            long maxUpdCount = 0;
            int portionCount;

            // todo
            // if (InitialLoading) ClearYourLocalRegionsDatabase();
            // else maxUpdCount = GetSavedUpdCount("region");

            var req = new GetRegionsChangesRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
            };

            WebServiceClient unistream = new WebServiceClient();


            try
            {
                do
                {
                    req.UpdateCount = maxUpdCount;
                    // Getting portion of data
                    var resp = unistream.GetRegionsChanges(req);

                    CheckFault(resp);

                    portionCount = resp.Regions.Length;

                    if (portionCount > 0)
                    {
                        // todo: InsertOrUpdateRegionsToYourLocalDatabase(resp.Banks);

                        maxUpdCount = resp.Regions
                            .Max(b => b.UpdateCount);

                        infPortionNo++;
                        infTotalCount += portionCount;

                        TestContext.WriteLine("Portion {0}, pCount {1}, tCount {2}, MaxUC {3}",
                                              infPortionNo, portionCount, infTotalCount, maxUpdCount);
                    }


                    // todo: consider saving unistream's resourcers. Remove next line when ready
                    if (infPortionNo > 5) break;

                } // while Xml is not empty
                while (portionCount > 0);

            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }

            TestContext.WriteLine("done");

            //todo: SaveUpdCount(maxUpdCount, "region");
            // Save UpdCount here to allow future differential data loading
        }



        [TestMethod]
        public void GetAllCountriesTestWcf2()
        {
            int infPortionNo = 0;
            int infTotalCount = 0;


            long maxUpdCount = 0;
            int portionCount;

            // todo
            // if (InitialLoading==true) ClearLocalCountriesDatabase();
            // // get previously saved UpdCount here to perform loading for only changed data
            // else maxUpdCount = GetSavedUpdCount("country");

            var req = new GetCountriesChangesRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
            };

            WebServiceClient unistream = new WebServiceClient();

            do
            {
                req.UpdateCount = maxUpdCount;
                // Getting portion of data
                var resp = unistream.GetCountriesChanges(req);

                CheckFault(resp);

                portionCount = resp.Countries.Length;

                if (portionCount > 0)
                {
                    // todo: InsertOrUpdateCountriesToYourLocalDatabase(resp.Countries);

                    maxUpdCount = resp.Countries
                        .Max(b => b.UpdateCount);

                    infPortionNo++;
                    infTotalCount += portionCount;

                    TestContext.WriteLine("Portion {0}, pCount {1}, tCount {2}, MaxUC {3}",
                                          infPortionNo, portionCount, infTotalCount, maxUpdCount);
                }


                // todo: consider saving unistream's resourcers. Remove next line when ready
                // if (infPortionNo > 10) break;

            } // while response is not empty
            while (portionCount > 0);

            TestContext.WriteLine("done");

            //todo: SaveUpdCount(maxUpdCount, "country");
            // Save UpdCount here to allow future differential data loading
        }




        [TestMethod]
        public void GetAllBankInfosTestWcf2()
        {
            int infPortionNo = 0;
            int infTotalCount = 0;


            long maxUpdCount = 0;
            int portionCount;

            // todo
            // if (InitialLoading==true) ClearLocalBankInfosDatabase();
            // else maxUpdCount = GetSavedUpdCount("bankinfos");

            var req = new GetBankInfosChangesRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
            };

            WebServiceClient unistream = new WebServiceClient();

            do
            {
                req.UpdateCount = maxUpdCount;
                // Getting portion of data
                var resp = unistream.GetBankInfosChanges(req);

                CheckFault(resp);

                portionCount = resp.BankInfos.Length;

                if (portionCount > 0)
                {
                    // todo: InsertOrUpdateBankInfosToYourLocalDatabase(resp.Countries);

                    maxUpdCount = resp.BankInfos
                        .Max(b => b.UpdateCount);

                    infPortionNo++;
                    infTotalCount += portionCount;


                    TestContext.WriteLine("xml={0}", Serialize(resp.BankInfos));
                    TestContext.WriteLine("Portion {0}, pCount {1}, tCount {2}, MaxUC {3}",
                                          infPortionNo, portionCount, infTotalCount, maxUpdCount);


                }


                // todo: consider saving unistream's resourcers. Remove next line when ready
                //if (infPortionNo > 10) break;

            } // while response is not empty
            while (portionCount > 0);

            TestContext.WriteLine("done");

            //todo: SaveUpdCount(maxUpdCount, "bankinfos");
            // Save UpdCount here to allow future differential data loading
        }




        [TestMethod]
        public void PrepareTransferTestWcf2()
        {


            WebServiceClient unistream = new WebServiceClient();
            try
            {
                TestContext.WriteLine("client.Endpoint.Address.Uri={0}", unistream.Endpoint.Address.Uri);


                Transfer transfer = new Transfer();

                transfer.Type = TransferType.Remittance;
                transfer.SentDate = DateTime.Now.Clarify();

                transfer.Amounts = new Amount[]
                                       {
                                           new Amount() {CurrencyID = 2, Sum = 123, Type = AmountType.Main},
                                           //new Amount() {CurrencyID = 1, Sum = 1000, Type = AmountType.ActualPaid}
                                       };

                //transfer.ControlNumber = "hello";

                transfer.Participators = new Participator[]
                                             {
                                                 new Participator()
                                                     {
                                                         ID = 187774,
                                                         Role = ParticipatorRole.SenderPOS
                                                     },
                                                 new Participator()
                                                     {
                                                         ID = 29,
                                                         Role = ParticipatorRole.ExpectedReceiverPOS
                                                     }
                                             };

                transfer.Consumers = new Consumer[] { };
                transfer.Consumers = new Consumer[]
                                         {
                                             new Consumer()
                                                 {
                                                     Person = new Person()
                                                                  {
                                                                      BirthDate = DateTime.MinValue,
                                                                      FirstName = "Джордж",
                                                                      MiddleName = "Уокер",
                                                                      LastName = "Буш",
                                                                      ID = 8451951
                                                                  },
                                                     Role = ConsumerRole.Sender
                                                 },
                                             new Consumer()
                                                 {
                                                     Person = new Person()
                                                                  {
                                                                      BirthDate = DateTime.MinValue,
                                                                      //FirstName = "Дмитрий",
                                                                      //MiddleName = "Анатольевич",
                                                                      //LastName = "Самарин",
                                                                      //FirstNameLat = "Dmitry",
                                                                      //MiddleNameLat = "Anatol'evich",
                                                                      //LastNameLat = "Samarin",
                                                                      //ID = 331518
                                                                      FirstName = "Джордж",
                                                                      MiddleName = "Уокер",
                                                                      LastName = "Буш",
                                                                      ID = 8451951
                                                                  },
                                                     Role = ConsumerRole.ExpectedReceiver
                                                 }
                                         };


                PrepareTransferRequestMessage req = new PrepareTransferRequestMessage()
                {
                    AuthenticationHeader = GetTestCreds(),
                    Transfer = transfer
                };


                var prepareResult = unistream.PrepareTransfer(req);

                CheckFault(prepareResult);

                var services = prepareResult.Transfer.Services.ToList();

                TestContext.WriteLine("Transfer.Comment={0}", prepareResult.Transfer.Comment);
                transfer.Comment = prepareResult.Transfer.Comment;
                TestContext.WriteLine("prepareResult.Transfer.ControlNumber={0}", prepareResult.Transfer.ControlNumber);
                transfer.ControlNumber = prepareResult.Transfer.ControlNumber;

                // выведем комиссии
                services.ForEach(
                    s =>
                    TestContext.WriteLine(
                        @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}", s.Fee,
                        s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID));
                services.ForEach(
                    s => s.Response = s.Mode == ServiceMode.Required ? Response.Accepted : Response.Rejected);



            }
            finally
            {
                if (unistream.State == System.ServiceModel.CommunicationState.Opened)
                    unistream.Close();
            }
        }


        [TestMethod]
        public void EstimateMainAmountTestWcf2()
        {
            WebServiceClient client = new WebServiceClient();




            Transfer transfer = new Transfer();

            transfer.Type = TransferType.Remittance;
            transfer.SentDate = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Unspecified);

            transfer.Amounts = new Amount[]
                                   {
                                       new Amount() { CurrencyID = 1, Sum = 0, Type = AmountType.Main },
                                   };

            transfer.Participators = new Participator[]
                                         {
                                             new Participator()
                                                 {
                                                     ID = 190475,
                                                     Role = ParticipatorRole.SenderPOS
                                                 },
                                             new Participator()
                                                 {
                                                     ID = 27,
                                                     Role = ParticipatorRole.ExpectedReceiverPOS
                                                 }
                                         };


            var req = new EstimateMainAmountRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
                Transfer = transfer,
                TotalAmount = 900

            };


            var estimateResult = client.EstimateMainAmount(req);

            CheckFault(estimateResult);


            TestContext.WriteLine("амоунты :");
            // выведем комиссии
            estimateResult.Transfer.Amounts.ToList().ForEach(
                a =>
                TestContext.WriteLine(
                    @"a.CurrencyID={0}, a.Type={1}, a.Sum={2}",
                    a.CurrencyID, a.Type, a.Sum));


            TestContext.WriteLine("Комиссии:");
            // выведем комиссии
            estimateResult.Transfer.Services.ToList().ForEach(
                s =>
                TestContext.WriteLine(
                    @"s.Fee={0}, s.CurrencyID={1}, s.ParticipatorID={2}, s.Mode={3}, s.ServiceID={4}", s.Fee,
                    s.CurrencyID, s.ParticipatorID, s.Mode, s.ServiceID));

        }

        public void CheckFault(WsResponse response)
        {
            // YOU MUST PERFORM AT LEAST THE FOLLOWING CHECK FOR EACH RESPONSE TAKEN FROM WEB SERVICE Global 2 
            if (response.Fault != null)
                throw new ApplicationException(
                    string.Format("Unistream error. Code={0}, ID={1}, Msg={2}",
                                  response.Fault.Code,
                                  response.Fault.ID,
                                  response.Fault.Message));
        }

        public static byte[] hash(string input)
        {
            var bytes = Encoding.ASCII.GetBytes(input);
            var hasher = new SHA256Managed();
            return hasher.ComputeHash(bytes);
        }

        [TestMethod]
        public void IdentifyPersonByCardTestWcf2()
        {
            WebServiceClient client = new WebServiceClient();

            var req = new IdentifyPersonByCardRequestMessage()
            {
                AuthenticationHeader = GetTestCreds(),
                CardCredentials = new CardCredentials()
                {
                    CardType = 1,
                    CardNumber = "007000014",
                    PinHash = hash("pincode here" + "007000014")
                }
            };

            var response = client.IdentifyPersonByCard(req);
            CheckFault(response);

            TestContext.WriteLine(
                "response.PersonID={0}, response.Template[0].BankID={1}, response.Template[0].LastName={2}, response.Template[0].PersonID={3}",
                response.PersonID, response.Templates[0].BankID, response.Templates[0].LastName, response.Templates[0].PersonID);
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// Gets the date component with unspecified kind.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime Clarify(this DateTime source)
        {
            return DateTime.SpecifyKind(source.Date, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Gets the datetime with unspecified kind.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime Clarify2(this DateTime source)
        {
            return DateTime.SpecifyKind(source, DateTimeKind.Unspecified);
        }

    }
}
