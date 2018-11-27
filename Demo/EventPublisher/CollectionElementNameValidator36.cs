using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Operations.ElementNameValidators;

namespace EventPublisher
{
    public class CollectionElementNameValidator36 : CollectionElementNameValidator, IElementNameValidator
    {
        bool IElementNameValidator.IsValidElementName(string elementName)
        {
            var isValid = base.IsValidElementName(elementName);
            if (!isValid && elementName[0] != '$' && elementName.IndexOf('.') != -1) return true;
            return isValid;
        }
    }
}