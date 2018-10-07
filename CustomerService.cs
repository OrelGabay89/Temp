using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.UI.WebControls;
using mBase.App.Shared.Utils;
using mBase.App.Shared.Utils.Enums;
using mBase.App.Shared.Utils.SearchEngin;
using mBase.App.Shared.Utils.Security;
using mBase.Entities.Models;
using Repository.Pattern.Repositories;
using Service.Pattern;

namespace mBase.App.Services
{
    interface ICustomerService : IService<T_Customers>
    {
        ListItem[] GetPriorities(int moduleId);
        ListItem[] GetStatusCrm();
        ListItem[] GetCustomerSource();
        ListItem[] GetCustomerType(int moduleId);
        ListItem[] GetUsers();
        ListItem[] GetActionTypeFilter();
        ListItem[] GetProjects();
        ListItem[] GetActions();
        ListItem[] GetAgents();
        ListItem[] GetStatuses(int moduleId);
        ListItem[] GetRegions();
        ListItem[] GetStatusMission();
        ListItem[] GetCustomerTerms();
        ListItem[] GetPaymentMethods();
        ListItem[] GetPredifnedDeliveryMethod();
        List<string> GetFilesToDelete(Guid currentCustomerId);
        bool IsCustomerOwnedByTheCurrentUser(Guid currentCustomerId, Guid userId);
        void DeleteCustomerAndFiles(Guid currentCustomerId);
        void DeleteCustumer(Guid currentCustomerId);
        List<string> GetSearchResultList(string txtSearch, string columnName, CustomerType customerType, bool filterIsValidAndOn);
        List<VS_CUSTOMERS> GetCustomers(bool search, string txtSearch, string columnName, int filterCount, CustomerType customerType, bool filterIsValidAndOn);
        void InsertEntityId(T_EntitiesId entitiesId);
    }
    public class CustomerService : Service<T_Customers>, ICustomerService
    {

        private readonly Guid _currentUserId;
        private readonly Guid _currentEntityId;
        private readonly IRepositoryAsync<T_Customers> _repository;

        public CustomerService(IRepositoryAsync<T_Customers> repository) : base(repository)
        {
            _repository = repository;
            _currentUserId = AppSettings.GetActiveUserId();
            _currentEntityId = AppSettings.GetActiveEntityId();
        }
        public ListItem[] GetPriorities(int moduleId)
        {
            return _repository.GetRepository<T_Priority>()
                .Queryable()
                .Where(p => p.ModuleId == moduleId)
                .ToList()
                        .Select(p => new ListItem(p.Priority, p.PriorityId.ToString()))
                        .ToArray();
        }
        public ListItem[] GetStatusCrm()
        {
            return _repository.GetRepository<T_StatusCRM>()
                .Queryable().ToList()
                  .Select(p => new ListItem(p.Status, p.StatusId.ToString()))
                  .ToArray();
        }
        public ListItem[] GetCustomerSource()
        {
            return _repository.GetRepository<T_CustomerSource>()
                .Queryable().ToList()
              .Select(p => new ListItem(p.CustomerSource, p.CustomerSourceId.ToString()))
              .ToArray();
        }
        public ListItem[] GetCustomerType(int moduleId)
        {
            return _repository.GetRepository<T_CustomerType>()
                .Queryable().ToList()
              .Where(p => p.ModuleId == moduleId)
              .ToList()
              .Select(p => new ListItem(p.CustomerType, p.CustomerTypeId.ToString()))
              .ToArray();
        }
        public ListItem[] GetUsers()
        {
            return _repository.GetRepository<User>()
                .Queryable().ToList()
               .Select(p => new ListItem(p.UserName, p.UserId.ToString()))
               .ToArray();
        }
        public ListItem[] GetActionTypeFilter()
        {
            return _repository.GetRepository<T_ActionTypeFilter>()
                .Queryable().ToList()
                  .Select(p => new ListItem(p.ActionTypeFilter, p.ActionTypeFilterId.ToString()))
                  .ToArray();

        }
        public ListItem[] GetProjects()
        {
            return _repository.GetRepository<T_Projects>()
                .Queryable().ToList()
                     .Select(p => new ListItem(p.ProjectName, p.ProjectId.ToString()))
                    .ToArray();

        }
        public ListItem[] GetActions()
        {
            return _repository.GetRepository<T_Actions>()
                .Queryable().ToList()
                    .Select(p => new ListItem(p.ActionType, p.ActionId.ToString()))
                    .ToArray();

        }
        public ListItem[] GetAgents()
        {
            return _repository.GetRepository<T_Agents>()
                .Queryable().ToList()
                  .Select(p => new ListItem(p.AgentName, p.AgentId.ToString()))
                  .ToArray();

        }
        public ListItem[] GetStatuses(int moduleId)
        {
            return _repository.GetRepository<T_Statuses>()
                .Queryable().ToList()
                .Where(p => p.ModuleId == moduleId)
                .ToList()
                   .Select(p => new ListItem(p.Status, p.StatusId.ToString()))
                   .ToArray();

        }
        public ListItem[] GetRegions()
        {
            return _repository.GetRepository<T_Regions>()
                .Queryable().ToList()
                   .Select(p => new ListItem(p.Region, p.RegionId.ToString()))
                   .ToArray();
        }
        public ListItem[] GetStatusMission()
        {
            return _repository.GetRepository<T_StatusMission>()
                .Queryable().ToList()
                   .Select(p => new ListItem(p.StatusMission, p.StatusMissionId.ToString()))
                   .ToArray();
        }
        public ListItem[] GetCustomerTerms()
        {
            return _repository.GetRepository<T_Terms>()
                .Queryable().ToList()
               .Select(p => new ListItem(p.TermName, p.TermId.ToString()))
               .ToArray();
        }
        public ListItem[] GetPaymentMethods()
        {
            return _repository.GetRepository<T_PaymentMethods>()
                .Queryable().ToList()
                    .Select(p => new ListItem(p.PaymentMethod, p.PaymentMethodId.ToString()))
                    .ToArray();

        }
        public ListItem[] GetPredifnedDeliveryMethod()
        {
            return _repository.GetRepository<T_PredefinedDeliveryMethod>()
                .Queryable().ToList()
                    .Select(p => new ListItem(p.DeliveryMethodName, p.PredefinedDeliveryMethodId.ToString()))
                    .ToArray();

        }
        public List<string> GetFilesToDelete(Guid currentCustomerId)
        {
            List<string> filesToDelete = new List<string>();

            var customerFiles = _repository.Queryable()
                .Include(m => m.T_EntitiesId.T_Files)
                .Select(x => x).FirstOrDefault(u => u.EntityId == currentCustomerId);
            if (customerFiles.T_EntitiesId != null)
                filesToDelete.AddRange(customerFiles.T_EntitiesId.T_Files.Select(file => file.FilePath));

            return filesToDelete;
        }
        public bool IsCustomerOwnedByTheCurrentUser(Guid currentCustomerId,Guid userId)
        {
            var customerBelongsToUser =  _repository.GetRepository<T_Customers>().Queryable().
                Any(p => p.EntityId == currentCustomerId && p.UserId == userId);

            var customerPurchaseServices =_repository.GetRepository<T_CustomerServices>().Queryable().
                Any(p => p.CustomerId == currentCustomerId);

            return customerBelongsToUser && customerPurchaseServices;
        }
        public void DeleteCustomerAndFiles(Guid currentCustomerId)
        {
            var filesToDelete = GetFilesToDelete(currentCustomerId);
            DeleteCustumer(currentCustomerId);

            foreach (string file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
        public void DeleteCustumer(Guid currentCustomerId)
        {
            var customer = Find(currentCustomerId);
            Delete(customer);
        }
        public List<string> GetSearchResultList(string txtSearch, string columnName, CustomerType customerType, bool filterIsValidAndOn)
        {
            var customers = GetCustomers(true, txtSearch, columnName, 100, customerType, filterIsValidAndOn);
            if (columnName.ToLower() == "customernumber")
            {
                List<Int32> results = customers
                                       .Select(x => x.GetType().GetProperty(columnName).GetValue(x))
                                       .Cast<int>()
                                       .ToList();
                return results.ConvertAll(delegate (int i) { return i.ToString(); });
            }
            else
            {
                List<string> results = customers
                                        .Select(x => x.GetType().GetProperty(columnName).GetValue(x))
                                        .Cast<string>()
                                        .ToList();
                return results;
            }

        }
        public List<VS_CUSTOMERS> GetCustomers(bool search, string txtSearch, string columnName, int filterCount, CustomerType customerType, bool filterIsValidAndOn)
        {
            List<VS_CUSTOMERS> lstCustomers = new List<VS_CUSTOMERS>();
            Guid currentUserId = AppSettings.GetActiveUserId();
            var companyId = AppSettings.GetCompanyId(currentUserId);
            var prop = typeof(VS_CUSTOMERS).GetProperty(columnName.ToLower(),
                                    BindingFlags.Public
                                    | BindingFlags.Instance
                                    | BindingFlags.IgnoreCase);
            AppSecurityUtil appSecurityUtil = new AppSecurityUtil();

            //Filtering is on.
            if (search)
            {
                FilterEngine filter = new FilterEngine();
                filter = filter.GetFilterByPropertyType(prop, txtSearch);
                List<FilterEngine> filters = new List<FilterEngine> { filter };

                var filterCriteria = ExpressionBuilder.GetExpression<VS_CUSTOMERS>(filters).Compile();

                //SuperAdmin
                if (appSecurityUtil.ValidateRoles())
                {
                    switch (customerType)
                    {
                        case CustomerType.All:
                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable().AsEnumerable()
                                .Where(filterCriteria)
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;
                        case CustomerType.Private:
                        case CustomerType.Business:

                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.CustomerTypeId == (int)customerType).AsEnumerable()
                                .Where(filterCriteria)
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;

                    }
                }
                else
                {
                    //Regular users
                    switch (customerType)
                    {
                        case CustomerType.All:
                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.UserId == currentUserId
                                            || (u.IsPrivate == false && u.CompanyId == companyId)).AsEnumerable()
                                .Where(filterCriteria)
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;
                        case CustomerType.Private:
                        case CustomerType.Business:

                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.UserId == currentUserId && u.CustomerTypeId == (int)customerType
                                            || (u.IsPrivate == false && u.CompanyId == companyId &&
                                                u.CustomerTypeId == (int)customerType)).AsEnumerable()
                                .Where(filterCriteria)
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;

                    }
                }

            }

            else
            {
                //No Filtering
                //SuperAdmin
                if (appSecurityUtil.ValidateRoles())
                {
                    switch (customerType)
                    {
                        case CustomerType.All:
                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable().AsEnumerable()
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;
                        case CustomerType.Private:
                        case CustomerType.Business:

                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.CustomerTypeId == (int)customerType).AsEnumerable()
                                .OrderByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;

                    }
                }
                else
                {
                    //Regular users
                    switch (customerType)
                    {
                        case CustomerType.All:
                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.UserId == currentUserId
                                            || (u.IsPrivate == false && u.CompanyId == companyId)).AsEnumerable()
                                .OrderBy(x=>x.CustomerCaseNumber)
                                .ThenByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;
                        case CustomerType.Private:
                        case CustomerType.Business:

                            lstCustomers = _repository.GetRepository<VS_CUSTOMERS>().Queryable()
                                .Where(u => u.UserId == currentUserId && u.CustomerTypeId == (int)customerType
                                            || (u.IsPrivate == false && u.CompanyId == companyId &&
                                                u.CustomerTypeId == (int)customerType)).AsEnumerable()
                                .OrderBy(x => x.CustomerCaseNumber)
                                .ThenByDescending(x => x.ModifiedDateTime).Take(filterCount)
                                .ToList();
                            break;

                    }
                }

            }

            if (filterIsValidAndOn)
            {
                if (FilterEngine.GetCurrentFilter != null && FilterEngine.GetCurrentFilter.Count > 0)
                {
                    var deleg1 =
                        ExpressionBuilder.GetExpression<VS_CUSTOMERS>(FilterEngine.GetCurrentFilter).Compile();
                    lstCustomers = lstCustomers.Where(deleg1).ToList();

                }
            }
            return lstCustomers;
        }

        public void InsertEntityId(T_EntitiesId entitiesId)
        {
            _repository.GetRepository<T_EntitiesId>().Insert(entitiesId);
        }
    }
}