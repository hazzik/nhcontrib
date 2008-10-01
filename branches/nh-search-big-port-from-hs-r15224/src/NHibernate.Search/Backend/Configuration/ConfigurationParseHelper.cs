using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace NHibernate.Search.Backend.Configuration
{
    /// <summary>
    /// Helper class to avoid managing NumberFormatException and similar code
    /// and ensure consistent error messages across Configuration parsing problems.
    /// </summary>
    public static class ConfigurationParseHelper
    {
        ///	<summary>
        /// Parses a String to get an int value. 
        /// </summary>
        ///	<param name="value"> A string containing an int value to parse </param>
        ///	<param name="errorMsgOnParseFailure"> message being wrapped in a SearchException if value is null or not correct. </param>
        ///	<returns> the parsed value </returns>
        ///	<exception cref="SearchException"> both for null values and for Strings not containing a valid int. </exception>
        public static int ParseInt(string @value, string errorMsgOnParseFailure)
        {
            if (@value == null)
            {
                throw new SearchException(errorMsgOnParseFailure);
            }
            try
            {
                return Convert.ToInt32(@value.Trim());
            }
            catch (Exception nfe)
            {
                throw new SearchException(errorMsgOnParseFailure, nfe);
            }
        }

        ///	 <summary> 
        /// In case value is null or an empty string the defValue is returned </summary>
        ///	<param name="value"> </param>
        ///	<param name="defValue"> </param>
        ///	<param name="errorMsgOnParseFailure"> </param>
        ///	<returns> the converted int. </returns>
        ///	<exception cref="SearchException"> if value can't be parsed. </exception>
        public static int ParseInt(string @value, int defValue, string errorMsgOnParseFailure)
        {
            if (string.IsNullOrEmpty(@value))
            {
                return defValue;
            }
            return ParseInt(@value, errorMsgOnParseFailure);
        }

        ///	 <summary>
        /// Looks for a numeric value in the Properties, returning
        ///	defValue if not found or if an empty string is found.
        ///	When the key the value is found but not in valid format
        ///	a standard error message is generated. </summary>
        ///	<param name="cfg"> </param>
        ///	<param name="key"> </param>
        ///	<param name="defValue"> </param>
        ///	<returns> the converted int. </returns>
        ///	<exception cref="SearchException"> for invalid format. </exception>
        public static int GetIntValue(NameValueCollection cfg, string key, int defValue)
        {
            string propValue = cfg[key];
            return ParseInt(propValue, defValue, "Unable to parse " + key + ": " + propValue);
        }

        ///	 <summary>
        /// Looks for a numeric value in the Properties, returning
        ///	defValue if not found or if an empty string is found.
        ///	When the key the value is found but not in valid format
        ///	a standard error message is generated. </summary>
        ///	<param name="cfg"> </param>
        ///	<param name="key"> </param>
        ///	<param name="defValue"> </param>
        ///	<returns> the converted int. </returns>
        ///	<exception cref="SearchException"> for invalid format. </exception>
        public static int GetIntValue(IDictionary<string,string> cfg, string key, int defValue)
        {
            string propValue = cfg[key];
            return ParseInt(propValue, defValue, "Unable to parse " + key + ": " + propValue);
        }

        ///	 <summary>
        /// Parses a string to recognize exactly either "true" or "false". </summary>
        ///	<param name="value"> the string to be parsed </param>
        ///	<param name="errorMsgOnParseFailure"> the message to be put in the exception if thrown </param>
        ///	<returns> true if value is "true", false if value is "false" </returns>
        ///	<exception cref="SearchException"> for invalid format or values. </exception>
        public static bool? ParseBoolean(string @value, string errorMsgOnParseFailure)
        {
            // avoiding Boolean.valueOf() to have more checks: makes it easy to spot wrong type in cfg.
            if (@value == null)
            {
                throw new SearchException(errorMsgOnParseFailure);
            }
            if ("false".Equals(@value.Trim(),StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
            if ("true".Equals(@value.Trim(), StringComparison.InvariantCultureIgnoreCase))
            
            {
                return true;
            }
            throw new SearchException(errorMsgOnParseFailure);
        }
    }
}