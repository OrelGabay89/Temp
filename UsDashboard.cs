#region Using

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using mBase.App.Controls.Charts;
using mBase.App.Services;
using mBase.App.Shared.Models;
using mBase.App.Shared.Utils;
using mBase.Entities.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Repository.Pattern.DataContext;
using Repository.Pattern.Ef6;
using Repository.Pattern.Repositories;
using Repository.Pattern.UnitOfWork;
using SimpleInjector.Lifestyles;
using Wisej.Web;

#endregion

namespace mBase.App.Modules.Customer.UserControls
{
    public partial class UsDashboard : Wisej.Web.UserControl
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private const string Json = "application/json";
        private readonly Guid _currentUserId;
        public UsDashboard()
        {
            InitializeComponent();

            LoadWidgets();

            _serializerSettings = new JsonSerializerSettings();
            _serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            _currentUserId = AppSettings.GetActiveUserId();
        }

        private void LoadWidgets()
        {

            using (AsyncScopedLifestyle.BeginScope(SimpleInjectorConfig.Container))
            {
                var smsSettingsService = SimpleInjectorConfig.Container.GetInstance<ISMSSettingsService>();
                SmsCounter.CounterValue = (float) smsSettingsService.GetSentSmsCount().Value;
            }


            //TODO extract to service
            var currentUserId = AppSettings.GetActiveUserId();
            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customerRepository = new Repository<T_Customers>(context, unitOfWork);
                dcCustomerCount.CounterValue = customerRepository.Queryable().Count(p => p.UserId == currentUserId);

                IRepositoryAsync<T_CustomerServices> customerServiceRepository =
                    new Repository<T_CustomerServices>(context, unitOfWork);
                dcPurchaseServiceCount.CounterValue =
                    customerServiceRepository.Queryable().Count(p => p.UserId == currentUserId);

                IRepositoryAsync<T_Transactions> transactionRepository = new Repository<T_Transactions>(context,
                    unitOfWork);

                var transactionDetailAmount = transactionRepository.Queryable()
                    .Include(p => p.T_TransactionPayments)
                    .Include(p => p.T_Customers)
                    .Where(p => p.T_Customers.UserId == currentUserId)
                    .GroupBy(p => p.T_Customers.UserId)
                    .Select(p =>
                   new
                        {
                            TotalAmount = p.Sum(x => x.TotalAmount),
                            AmountPaid = p.Sum(x => x.T_TransactionPayments.Count > 0 ?
                            p.Sum(z => z.T_TransactionPayments.Sum(s => s.AmountPaid)) : 0)
                   }).ToList();

                var totalAmountDetail = new { TotalAmount = transactionDetailAmount.Sum(p => p.TotalAmount), AmountPaid = transactionDetailAmount.Sum(p => p.AmountPaid) };
                dcPaymentCount.CounterValue = float.Parse(totalAmountDetail.AmountPaid.ToString());
                dcBalanceCount.CounterValue = float.Parse((totalAmountDetail.TotalAmount - totalAmountDetail.AmountPaid).ToString());

                dcPaymentCount.PictureCurrencyVisible = true;
                dcBalanceCount.PictureCurrencyVisible = true;
            }
        }



        private void ctlLineChart1_WebRequest(object sender, WebRequestEventArgs e)
        {
            ctrlLineChartCutomerByMonth.ChartTitle = Shared.ResourceFiles.General.CustomersByMonth;
            ctrlLineChartCutomerByMonth.PointOptionFormatOptionFormat = PointOptionFormat.PointCount;

            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customersRepository = new Repository<T_Customers>(context, unitOfWork);

                var month = new[]
                    {
                        Shared.ResourceFiles.General.January,Shared.ResourceFiles.General.February,Shared.ResourceFiles.General.March,
                        Shared.ResourceFiles.General.April,Shared.ResourceFiles.General.May,Shared.ResourceFiles.General.June,
                        Shared.ResourceFiles.General.July,Shared.ResourceFiles.General.August,Shared.ResourceFiles.General.September,
                        Shared.ResourceFiles.General.October,Shared.ResourceFiles.General.November,Shared.ResourceFiles.General.December
                    };

                ctrlLineChartCutomerByMonth.Categories.AddRange(month);

                var currentUserId = AppSettings.GetActiveUserId();

                var count = (from p in customersRepository.Queryable()
                             where p.UserId == currentUserId
                             select p).Count();

                var results =
                    customersRepository.Queryable()
                        .Where(u => u.UserId == currentUserId)
                         .GroupBy(o => new
                         {
                             Month = o.CreatedDate.Month,
                             Year = o.CreatedDate.Year
                         })
                        .Select(g => new
                        {
                            Month = g.Key.Month,
                            Year = g.Key.Year,
                            Percent = (g.Count() * 100.0) / count,
                        }).OrderBy(a => a.Year).ThenBy(a => a.Month).ToList();


                IEnumerable<int> yearList = results.Select(x => x.Year).Distinct().ToList();
                var objList = new List<double>();


                foreach (var year in yearList)
                {
                    var overLoopData = results.Where(x => x.Year == year);

                    for (int i = 1; i <= 12; i++)
                    {
                        if (year == overLoopData.Select(x => x.Year).FirstOrDefault()
                            && overLoopData.Select(x => x.Month).FirstOrDefault() == i)
                        {
                            objList.Add(Math.Round(overLoopData.Select(x => x.Percent).FirstOrDefault(), 2));
                        }
                        else
                        {
                            objList.Add(0);
                        }

                        if (i == 12)
                        {
                            ctrlLineChartCutomerByMonth.Series.Add(new SeriesItem { Data = objList, Name = year.ToString() });
                            objList = new List<double>();
                            yearList = yearList.Except(new[] { year });
                        }
                    }

                }

                e.Response.ContentType = Json;
                var json = JsonConvert.SerializeObject(ctrlLineChartCutomerByMonth.Data, _serializerSettings);
                e.Response.Write(json);

            }
        }

        private void ctrlPieChart1_WebRequest(object sender, WebRequestEventArgs e)
        {
            #region ctrlPieChart

            ctrlPieChartCutomerByRegion.ChartTitle = Shared.ResourceFiles.General.CustomersByArea;
            ctrlPieChartCutomerByRegion.PointOptionFormatOptionFormat = PointOptionFormat.PointCount;

            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customersRepository =
                    new Repository<T_Customers>(context, unitOfWork);
                {
                    ctrlPieChartCutomerByRegion.ChartTitle = Shared.ResourceFiles.General.CustomersByArea;

                    var count = (from p in customersRepository.Queryable()
                        where p.UserId == _currentUserId
                                 select p).Count();

                    var results =
                        customersRepository.Queryable()
                            .Where(u => u.UserId == _currentUserId)
                            .Include(m => m.T_Regions)
                            .GroupBy(d => d.RegionId, f => f.T_Regions.Region)
                            .Select(grp => new
                            {
                                RegionId = grp.Key,
                                Region = grp.FirstOrDefault(),
                                Percent = (grp.Count()*100.0)/count
                            });


                    foreach (var series in results)
                    {
                        ctrlPieChartCutomerByRegion.Series.Add(new SeriesItem
                        {
                            Data = new List<double> { Math.Round(series.Percent, 2) },
                            Name = series.Region
                        });

                    }
                }

                e.Response.ContentType = Json;
                var json = JsonConvert.SerializeObject(ctrlPieChartCutomerByRegion.Data, _serializerSettings);
                e.Response.Write(json);
            }

            #endregion
        }

        private void ctrlColumnWithDrilldown1_WebRequest(object sender, WebRequestEventArgs e)
        {
            #region ColumnWithDrilldown

            ctrlCWDDCustomerStatuses.ChartTitle = Shared.ResourceFiles.General.CustomersStatuses;
            ctrlCWDDCustomerStatuses.PointOptionFormatOptionFormat = PointOptionFormat.PointCount;

            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customersRepository =
                    new Repository<T_Customers>(context, unitOfWork);
                {

                    var count = (from p in customersRepository.Queryable()
                                 where p.UserId == _currentUserId
                                 select p).Count();

                    var results =
                        customersRepository.Queryable()
                            .Where(u => u.UserId == _currentUserId)
                            .Include(m => m.T_Statuses)
                            .GroupBy(d => d.StatusId, f => f.T_Statuses.Status)
                            .Select(grp => new
                            {
                                StatusId = grp.Key,
                                Status = grp.FirstOrDefault(),
                                Percent = (grp.Count() * 100.0) / count
                            });


                    foreach (var series in results)
                    {
                        ctrlCWDDCustomerStatuses.Series.Add(new SeriesItem
                        {
                            Data = new List<double> { Math.Round(series.Percent, 2) },
                            Name = series.Status
                        });

                    }
                }



                e.Response.ContentType = Json;
                var json = JsonConvert.SerializeObject(ctrlCWDDCustomerStatuses.Data, _serializerSettings);
                e.Response.Write(json);
            }

            #endregion
        }

        private void ctrlSCDChartCustomerPriority_WebRequest(object sender, WebRequestEventArgs e)
        {
            #region ctrlSemiCircleDonutChart

            {
                ctrlSCDChartCustomerPriority.ChartTitle = Shared.ResourceFiles.General.CustomersPriority;
                ctrlSCDChartCustomerPriority.PointOptionFormatOptionFormat = PointOptionFormat.PointDotPercent;

                using (IDataContextAsync context = new mBaseMyBusinessContext())
                using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
                {
                    IRepositoryAsync<T_Customers> customersRepository =
                        new Repository<T_Customers>(context, unitOfWork);
                    {

                        var count = (from p in customersRepository.Queryable()
                            where p.UserId == _currentUserId
                            select p).Count();

                        var results =
                            customersRepository.Queryable()
                                .Where(u => u.UserId == _currentUserId)
                                .GroupBy(d => d.CustomerPriorityId, f => f.T_Priority.Priority)
                                .Select(grp => new
                                {
                                    CustomerPriorityId = grp.Key,
                                    Priority = grp.FirstOrDefault(),
                                    Percent = (grp.Count()*100.0)/count
                                });


                        foreach (var series in results)
                        {
                            ctrlSCDChartCustomerPriority.Series.Add(new SeriesItem
                            {
                                Data = new List<double> {series.Percent},
                                Name = series.Priority
                            });

                        }
                    }

                    e.Response.ContentType = Json;
                    var json = JsonConvert.SerializeObject(ctrlSCDChartCustomerPriority.Data, _serializerSettings);
                    e.Response.Write(json);

                }
            }

            #endregion
        }

        private void ctrlPieChartCustomerSource_WebRequest(object sender, WebRequestEventArgs e)
        {
            #region ctrlPieChart

            ctrlPieChartCustomerSource.ChartTitle = Shared.ResourceFiles.General.CustomersBySource;
            ctrlPieChartCustomerSource.PointOptionFormatOptionFormat = PointOptionFormat.PointDotPercent;

            ctrlPieChartCustomerSource.PointOptionFormatOptionFormat = PointOptionFormat.PointCount;
            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customersRepository =
                    new Repository<T_Customers>(context, unitOfWork);
                {
                    
                    var count = (from p in customersRepository.Queryable()
                                 where p.UserId == _currentUserId
                                 select p).Count();

                    var results =
                        customersRepository.Queryable()
                            .Where(u => u.UserId == _currentUserId)
                            .Include(m => m.T_CustomerSource)
                            .GroupBy(d => d.CustomerSourceId, f => f.T_CustomerSource.CustomerSource)
                            .Select(source => new
                            {
                                CustomerSourceId = source.Key,
                                CustomerSource = source.FirstOrDefault(),
                                Percent = (source.Count() * 100.0) / count
                            });


                    foreach (var series in results)
                    {
                        ctrlPieChartCustomerSource.Series.Add(new SeriesItem
                        {
                            Data = new List<double> { Math.Round(series.Percent,2) },
                            Name = series.CustomerSource
                        });

                    }
                }

                e.Response.ContentType = Json;
                var json = JsonConvert.SerializeObject(ctrlPieChartCustomerSource.Data, _serializerSettings);
                e.Response.Write(json);
            }

            #endregion
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            DateTime dtStart = DateTime.Today;
            DateTime dtEnd = DateTime.Today;

            var currentUserId = AppSettings.GetActiveUserId();
            using (IDataContextAsync context = new mBaseMyBusinessContext())
            using (IUnitOfWorkAsync unitOfWork = new UnitOfWork(context))
            {
                IRepositoryAsync<T_Customers> customerRepository = new Repository<T_Customers>(context, unitOfWork);
                dcCustomerCount.CounterValue = customerRepository.Queryable(
                    
                    ).Count(p => p.UserId == currentUserId 
                    && (p.CreatedDate>=dtStart.Date
                    && p.CreatedDate <= dtEnd.Date));

                IRepositoryAsync<T_CustomerServices> customerServiceRepository = new Repository<T_CustomerServices>(context, unitOfWork);
                dcPurchaseServiceCount.CounterValue = customerServiceRepository.Queryable( ).Count(p => p.UserId == currentUserId);

                IRepositoryAsync<T_Transactions> transactionRepository = new Repository<T_Transactions>(context,
                      unitOfWork);

                var transactionDetailAmount = transactionRepository.Queryable()
                        .Include(p => p.T_TransactionPayments)
                        .Include(p => p.T_Customers)
                        .Where(p => p.T_Customers.UserId == currentUserId 
                         && p.TransactionDate >= dtStart.Date && p.TransactionDate <= dtEnd.Date)
                        .GroupBy(p => p.T_Customers.UserId).Select(p =>
                        new
                        {
                            TotalAmount = p.Sum(x => x.TotalAmount),
                            AmountPaid = p.Sum(x => x.T_TransactionPayments.Sum(s => s.AmountPaid))
                        }).ToList();
                var totalAmountDetail = new { TotalAmount = transactionDetailAmount.Sum(p => p.TotalAmount), AmountPaid = transactionDetailAmount.Sum(p => p.AmountPaid) };
                dcPaymentCount.CounterValue = float.Parse(totalAmountDetail.AmountPaid.ToString());
                dcBalanceCount.CounterValue = float.Parse((totalAmountDetail.TotalAmount - totalAmountDetail.AmountPaid).ToString());
            }
        }

    }
}