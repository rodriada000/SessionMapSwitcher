using SessionModManagerCore.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionModManagerWPF.Classes
{
    /// <summary>
    /// Class to compare DateTime for sorting <see cref="AssetViewModel.UpdatedDate"/>
    /// </summary>
    /// <remarks>
    /// reference: https://stackoverflow.com/questions/4734055/c-sharp-icomparer-if-datetime-is-null-then-should-be-sorted-to-the-bottom-no
    /// </remarks>
    public class DateTimeComparer : IComparer
    {
        public ListSortDirection SortDirection = ListSortDirection.Ascending;

        public int Compare(DateTime? x, DateTime? y)
        {
            DateTime nx = x ?? DateTime.MinValue;
            DateTime ny = y ?? DateTime.MinValue;

            return nx.CompareTo(ny);
        }

        public int Compare(object x, object y)
        {
            AssetViewModel ax = (x as AssetViewModel);
            AssetViewModel ay = (y as AssetViewModel);

            DateTime.TryParse(ax?.UpdatedDate, out DateTime dx);
            DateTime.TryParse(ay?.UpdatedDate, out DateTime dy);

            if (SortDirection == ListSortDirection.Ascending)
            {
                return Compare(dx, dy);
            }
            else
            {
                return Compare(dy, dx);
            }
        }
    }
}
