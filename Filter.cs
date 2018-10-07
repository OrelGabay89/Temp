using System;
using System.Collections.Generic;
using System.Reflection;
using Wisej.Base;
using Wisej.Web;

namespace mBase.App.Shared.Utils.SearchEngin
{
    public class FilterEngine
    {
        public string PropertyName { get; set; }
        public Op Operation { get; set; }
        public object Value { get; set; }

        public FilterEngine GetFilterByPropertyType(PropertyInfo prop, string text)
        {
            FilterEngine filter = new FilterEngine { PropertyName = prop.Name.ToLower() };


            switch (prop.PropertyType.ToString())
            {
                case "System.Int32":

                    filter.Operation = Op.Equals;

                    if (text.IsDigitsOnly())
                    {
                        filter.Value = Convert.ToInt32(text);
                    }
                    else
                    {
                        filter.Value = -1;
                    }

                    break;

                case "System.String":

                    filter.Operation = Op.Contains;
                    filter.Value = text.ToLower();

                    break;


                case "global::System.DateTime":

                    filter.Operation = Op.LessThanOrEqual;
                    filter.Value = text;
                    break;

                case "System.Double":

                    filter.Operation = Op.Equals;

                    if (text.IsDigitsOnly())
                    {
                        filter.Value = Convert.ToDouble(text);
                    }
                    else
                    {
                        filter.Value = -1;
                    }
                    break;

                case "System.Nullable`1[System.Boolean]":
                case "System.Boolean":

                    filter.Operation = Op.Equals;

                    Boolean parsedValue;

                    if (Boolean.TryParse(text.YesNoByBooleanValue(), out parsedValue))
                    {
                        filter.Value = parsedValue;
                    }
                    else
                    {
                        filter.Value = "false";
                    }



                    break;

            }
            return filter;
        }

        public FilterEngine CreateFilterByOp(Control controlType, string propertyName, Op operation, string value)
        {
            FilterEngine filter = null;
            switch (operation)
            {
                case Op.Equals:
                case Op.GreaterThan:
                case Op.GreaterThanOrEqual:
                case Op.LessThanOrEqual:
                case Op.LessThan:
                case Op.NotEquals:
                    filter = new FilterEngine
                    {
                        PropertyName = propertyName,
                        Operation = operation
                    };

                    switch (controlType.GetType().Name)
                    {
                        case "TextBox":
                            filter.Value = Convert.ToInt32(value);
                            break;

                       
                    }
                    break;

                case Op.Contains:
                case Op.StartsWith:
                case Op.EndsWith:

                    filter = new FilterEngine
                    {
                        PropertyName = propertyName,
                        Operation = operation,
                        Value = value
                    };
                    break;
            }
            return filter;
        }

        public static List<FilterEngine> GetCurrentFilter
        {
            get
            {
                var filters = ApplicationBase.Session["Filters"] as List<FilterEngine>;

                return filters;
            }
        }
    }
    public enum Op
    {
        Equals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        NotEquals
    }

}