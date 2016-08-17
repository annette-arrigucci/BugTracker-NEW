using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class TicketDetailsViewModelComparer : IEqualityComparer<TicketDetailsViewModel>
    {
        public bool Equals(TicketDetailsViewModel x, TicketDetailsViewModel y)
        {
            //Check whether the objects are the same object. 
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether the products' properties are equal. 
            return x != null && y != null && x.Id.Equals(y.Id) && x.Title.Equals(y.Title);
        }

        public int GetHashCode(TicketDetailsViewModel obj)
        {
            //Get hash code for the Id field 
            int hashProductName = obj.Id.GetHashCode();

            //Get hash code for the Title field. 
            int hashProductCode = obj.Title.GetHashCode();

            //Calculate the hash code for the product. 
            return hashProductName ^ hashProductCode;
        }

    }
}