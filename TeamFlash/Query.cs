using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Collections;
using System.Xml.Linq;
using System.Net;
using System.Xml;

namespace TeamFlash
{
    public class Query : DynamicObject, IEnumerable
    {
        readonly string baseUrl;
        readonly string username;
        readonly string password;
        XDocument document;
        XDocument childDocument;

        public Query(
            string baseUrl,
            string username,
            string password,
            XDocument document = null)
        {
            this.baseUrl = baseUrl;
            this.username = username;
            this.password = password;
            this.document = document;
        }

        public string RestBasePath = @"/httpAuth/app/rest/";
        const string ExistsKeyword = "exists";

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var bindingName = binder.Name.Replace("_", "-").ToLower();

            if (bindingName.EndsWith(ExistsKeyword))
            {
                bindingName = bindingName.Substring(0, bindingName.Length - ExistsKeyword.Length);

                object unusedResult;
                result = TryFind(bindingName, out unusedResult);
                return true;
            }

            return TryFind(bindingName, out result);
        }

        bool TryFind(string bindingName, out object result)
        {
            if (document == null)
            {
                Load(bindingName);
                result = new Query(baseUrl, username, password, document);
                return true;
            }

            if (bindingName.Equals("first", StringComparison.CurrentCultureIgnoreCase))
            {
                var enumerator = GetEnumerator();
                if (enumerator.MoveNext())
                {
                    result = enumerator.Current;
                    return true;
                }

                result = null;
                return true;
            }

            string value;
            IEnumerable<XElement> selectedDecendants;
            if (!TryFind(bindingName, document, out value, out selectedDecendants))
            {
                if (childDocument == null)
                {
                    if (!TryRetrieveChildDocument(document, out childDocument))
                    {
                        result = null;
                        return false;
                    }
                }

                if (childDocument == null)
                {
                    result = null;
                    return false;
                }

                if (!TryFind(bindingName, childDocument, out value, out selectedDecendants))
                {
                    result = null;
                    return true;
                }
            }

            if (value != null)
            {
                result = value;
                return true;
            }

            if (selectedDecendants != null)
            {
                if (selectedDecendants.Count() == 1 &&
                    !selectedDecendants.First().HasElements &&
                    bindingName.Equals(selectedDecendants.First().Name.LocalName, StringComparison.CurrentCultureIgnoreCase))
                {
                    XDocument stepChildDocument;
                    if (TryRetrieveChildDocument(selectedDecendants.First(), out stepChildDocument))
                    {
                        result = new Query(baseUrl, username, password, stepChildDocument);
                        return true;
                    }
                    result = selectedDecendants.First().Value;
                    return true;
                }
                result = new Query(baseUrl, username, password, new XDocument(selectedDecendants));
                return true;
            }

            result = null;
            return false;
        }

        public void Load(string relativeUrl = "")
        {
            var url = baseUrl + RestBasePath + relativeUrl;
            document = Retrieve(url);
        }

        bool TryRetrieveChildDocument(XDocument parentDocument, out XDocument childDocument)
        {
            var firstElement = parentDocument.Descendants().First();
            return TryRetrieveChildDocument(firstElement, out childDocument);
        }

        bool TryRetrieveChildDocument(XElement element, out XDocument childDocument)
        {
            string childHref;

            if (!TryFindAttributeValueByName("href", element, out childHref))
            {
                childDocument = null;
                return false;
            }

            var queryUrl = baseUrl + childHref;
            childDocument = Retrieve(queryUrl);
            return true;
        }

        bool TryFind(string bindingName, XDocument parentDocument, out string value, out IEnumerable<XElement> selectedDecendants)
        {
            selectedDecendants = null;
            var firstElement = parentDocument.Descendants().First();
            return (TryFindAttributeValueByName(bindingName, firstElement, out value) ||
                    TryFindDecendants(bindingName, parentDocument, out selectedDecendants));
        }

        static bool TryFindDecendants(string name, XDocument doc, out IEnumerable<XElement> selectedDecendants)
        {
            selectedDecendants = from item in doc.Descendants()
                                 where item.Name.LocalName.Equals(name, StringComparison.CurrentCultureIgnoreCase)
                                 select item;

            if (!selectedDecendants.Any())
            {
                selectedDecendants = null;
                return false;
            }

            return true;
        }

        static bool TryFindAttributeValueByName(string name, XElement element, out string selectedAttributeValue)
        {
            var valueByName = (from attribute in element.Attributes()
                               select new
                               {
                                   Key = attribute.Name.LocalName.ToLower(),
                                   attribute.Value
                               }).ToDictionary(k => k.Key, v => v.Value);

            return valueByName.TryGetValue(name.ToLower(), out selectedAttributeValue);
        }

        WebClient client;

        XDocument Retrieve(string queryUrl)
        {
            if (client == null)
            {
                client = new WebClient
                {
                    Credentials = new NetworkCredential(username, password)
                };
                client.Headers.Add("Accepts:text/xml");
            }

            try
            {
                using (var stream = client.OpenRead(queryUrl))
                using (var reader = XmlReader.Create(stream, new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore }))
                {
                    return XDocument.Load(reader);
                }
            }
            catch
            {
                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return GetAllChildren().GetEnumerator();
        }

        List<Query> GetAllChildren()
        {
            var parentDocument = document;
            var secondElement = parentDocument.Descendants().Skip(1);
            var childCount = secondElement.Count();
            if (childCount == 0)
            {
                if (childDocument == null)
                {
                    if (!TryRetrieveChildDocument(document, out childDocument))
                    {
                        return new List<Query>();
                    }
                }

                parentDocument = childDocument;
                secondElement = parentDocument.Descendants().Skip(1);
                childCount = secondElement.Count();
                if (childCount == 0)
                {
                    return new List<Query>();
                }
            }

            var childName = secondElement.First().Name.LocalName;
            var decendants = from decendant in parentDocument.Descendants(childName)
                             select new Query(baseUrl, username, password, new XDocument(decendant));
            return decendants.ToList();
        }
    }
}
