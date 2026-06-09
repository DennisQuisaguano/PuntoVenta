namespace Blazor.Helpers;

public class PaginationHelper
{
    public static List<int> GenerateVisiblePages(int currentPage, int totalPages)
    {
        var pages = new List<int>();

        if (totalPages <= 7)
        {
            for (int i = 1; i <= totalPages; i++) pages.Add(i);
            return pages;
             
        }

        pages.Add(1);

        if (currentPage > 3) pages.Add(-1);

        int start = Math.Max(2, currentPage -1);
        int end = Math.Min(totalPages - 1, currentPage + 1);

        for (int i = start; i <= end; i++) pages.Add(i);

        if (currentPage < totalPages -2 ) pages.Add(-1);

        pages.Add(totalPages);

        return pages.Distinct().ToList();

    }
}
