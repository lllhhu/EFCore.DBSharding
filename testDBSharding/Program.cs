using EntityFrameworkCore.DBSharding;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace testDBSharding
{
    //订单状态
    public enum OrderStatus
    {
        /// <summary>
        /// 等待付款（前台下单未支付未取消）
        /// </summary>
        WaitPaying = 1,
        /// <summary>
        /// 已付款（下单已支付未取消）
        /// </summary>

        HadPay = 2,
        /// <summary>
        /// 待发货（物服更改）
        /// </summary>

        WaiDelivery = 3,
        /// <summary>
        /// 已发货 等待转账
        /// </summary>

        HadDeliveryWaitForTransfer = 4,
        /// <summary>
        /// 结单 交易完成
        /// </summary>

        Check = 5,
        /// <summary>
        /// 已退款
        /// </summary>

        HadRefund = 6,
        /// <summary>
        /// 已取消
        /// </summary>
        Cancel = 7,
        /// <summary>
        /// 发货失败等待退款
        /// </summary>
        DeliveryFailedWaitForDrawback = 8,
    }

    public class GameAccountInfo
    {
        public GameAccountInfo()
        {

        }

        public GameAccountInfo(string gameAccount, string gamePassword, string gameInfo)
        {
            this.GameAccount = gameAccount;
            this.GamePassword = GamePassword;
            this.GameAccount = gameInfo;
        }

        public string GameAccount
        {
            get; private set;
        }

        public string GamePassword
        {
            get; private set;
        }

        public string GameInfo
        {
            get; private set;
        }
    }


    /// <summary>
    /// 在Order上下文，是一个值对象
    /// </summary>
    public class BizofferInfo
    {
        public BizofferInfo()
        {

        }

        public BizofferInfo(string bizofferId, string name, decimal price)
        {
            if (string.IsNullOrWhiteSpace(name) || price < 0)
            {
                throw new ArgumentNullException("参数不能为空");
            }

            this.BizofferId = bizofferId;
            this.Name = name;
            this.Price = price;

        }

        /// <summary>
        /// 供应商商品Id
        /// </summary>
        public string BizofferId
        {
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        public decimal Price
        {
            get; private set;
        }


    }

    public class Order
    {
        public Order()
        { }

        public Order(string buyerId, string buyerName)
        {
            this.Id = Guid.NewGuid().ToString();
            this.BuyerId = buyerId;
            this.BuyName = buyerName;
            this.OrderItems = new List<OrderItem>();
            this.SetStatusToWaitPaying();
        }

        public string Id
        {
            get; private set;
        }

        public string Name
        {
            get; private set;
        }

        public decimal TotalPrice
        {
            get; private set;
        }

        public string BuyerId
        {
            get; private set;
        }

        public string BuyName
        {
            get; private set;
        }

        public OrderStatus Status
        {
            get; private set;
        }

        public virtual ICollection<OrderItem> OrderItems
        {
            get; private set;
        }

        public void AddItem(BizofferInfo info)
        {
            if (info == null) throw new Exception("发布单信息不能为空");
            if (info.Price <= 0) throw new Exception("发布单价格不能小于等于0");

            //由于order和orderItem是个聚合关系,order是这个聚合的聚合根，它负责业务规则的不变性和数据的一致性
            //下面逻辑体现了以上原则

            this.TotalPrice = this.TotalPrice + info.Price;//数据的一致性


            var items = this.OrderItems.Where(m => m.BizofferInfo.BizofferId == info.BizofferId).ToList();   //业务的不变性，如果订单项列表中已有该商品，则订单项商品数加一即可
            if (items.Count > 0)
            {
                items[0].IncrementQuantity();
            }
            else
            {

                OrderItem item = new OrderItem(info);
                this.OrderItems.Add(item);
            }


        }

        //设置状态为等待付款
        public void SetStatusToWaitPaying()
        {
            this.Status = OrderStatus.WaitPaying;
        }

        //设置状态为已支付
        public void SetStatusToHadPay()
        {
            //如果有逻辑写在这里
            this.Status = OrderStatus.HadPay;
        }


    }
    public class OrderItem
    {
        public OrderItem(BizofferInfo bizofferInfo)
        {
            this.BizofferInfo = bizofferInfo;
            this.Id = Guid.NewGuid().ToString();
        }


        public OrderItem()
        {

        }

        public string Id
        {
            get; private set;
        }

        public BizofferInfo BizofferInfo
        {
            get; private set;
        }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity
        {
            get; private set;
        }

        public void IncrementQuantity(int quantity = 1)
        {
            if (quantity <= 0) throw new Exception("订单项数量不能小于0");
            this.Quantity = this.Quantity + quantity;
        }

        public virtual Order Order
        {
            get; private set;
        }
    }

    public class MQTTUser
    {
        //[BsonRepresentation(BsonType.ObjectId)]
        //public string _id { get; set; }
        [Key]
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public string Topic { get; private set; }
        public string GroupId { get; private set; }
        public string Token { get; private set; }
        public long ExpireTime { get; private set; }
        public string Actions { get; private set; }
        public DateTime CreateDateTime { get; private set; }
        public DateTime LastModify { get; private set; }
        public int DaysOverdue { get; private set; }

        public MQTTUser(string UserId, string UserName, string Token, string topic, string groupId, long expireTime, string actions, int DaysOverdue)
        {
            this.UserId = UserId;
            this.UserName = UserName;
            this.Token = Token;
            this.Topic = topic;
            this.GroupId = groupId;
            this.ExpireTime = expireTime;
            this.Actions = actions;
            this.CreateDateTime = DateTime.Now;
            this.LastModify = this.CreateDateTime;
            this.DaysOverdue = DaysOverdue;
        }


        public void SetToken(string token, long expireTime)
        {
            this.Token = token;
            this.ExpireTime = expireTime;
            this.LastModify = DateTime.Now;
        }
    }

    public class AppContext : DbContext
    {
        //public AppContext(DbContextOptions<AppContext> options) : base(options)
        //{
        //    this.Database.EnsureCreated();
        //}
        //public AppContext()
        //{

        //}

        //由于在 Startup中配置了上下文池的依赖注入，所以就不需要在上下文中实现配置相关，否则会报异常
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // LoggerFactory loggerFactory = new LoggerFactory();
            // loggerFactory.AddConsole(LogLevel.Debug);
            // optionsBuilder.UseLoggerFactory(loggerFactory);

            //var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            //{
            //    DataSource = "127.0.0.1",
            //    InitialCatalog = "GameId2",
            //    UserID = "sa",
            //    Password = "q#@!123q"
            //};


            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "192.168.130.70",
                InitialCatalog = "MQTTAuth",
                UserID = "qctest",
                Password = "//5173@#"
            };


            //optionsBuilder.UseSqlServer(sqlConnectionStringBuilder.ConnectionString);
            optionsBuilder.UseMySql(sqlConnectionStringBuilder.ConnectionString);
            //optionsBuilder.UseLazyLoadingProxies();


            base.OnConfiguring(optionsBuilder);
        }


        //public DbSet<GameId.Domain.Bizoffer.Bizoffer> Bizoffer { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<MQTTUser> MQTTUser { get; set; }
        //public DbSet<Seller> Seller { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            #region 
            //modelBuilder.Entity<GameId.Domain.Bizoffer.Bizoffer>().ToTable("Bizoffer");
            //modelBuilder.Entity<GameId.Domain.Bizoffer.Seller>().ToTable("Seller");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");
            modelBuilder.Entity<OrderItem>().OwnsOne(c => c.BizofferInfo);//（值对象）复杂类型映射
            modelBuilder.Entity<Order>().HasMany(m => m.OrderItems);// 使用简单方式配置一对多关系

            modelBuilder.Entity<MQTTUser>().ToTable("MQTTUser");
            #endregion


            base.OnModelCreating(modelBuilder);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {

            List<ShardingRule> shardingRules = new List<ShardingRule>();
            shardingRules.Add(new ModShardingRule() { TableName = "MQTTUser", Mod = 3, ShardingKey = "UserId" });
            //shardingRules.Add(new DateShardingRule() { TableName = "Bizoffer", ShardingKey = "CreateTime" });
            //shardingRules.Add(new ModShardingRule() { TableName = "Order", Mod = 3, ShardingKey = "Id" });
            //shardingRules.Add(new ModShardingRule() { TableName = "OrderItem", Mod = 3, ShardingKey = "OrderId" });

              DiagnosticListener.AllListeners.Subscribe(new CommandListener(shardingRules));

            Console.WriteLine(DateTime.Now);
            using (AppContext dbContext = new AppContext())
            {
                //dbContext.Database.EnsureCreated();

                var order = dbContext.MQTTUser.Find("a8f62f91-eead-40f0-959f-4bcbb88482de");
                //var order = dbContext.Order.Where(m=>m.Id== "1d47ea8f-4c34-4554-a2ad-bedca76546a8").FirstOrDefault();

            }
            Console.WriteLine(DateTime.Now);

            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}
