using System.Net.Http;

namespace Mirage.rest
{
    /// <summary>
    /// REST Interface.
    /// Implemented within each class in the rest directory.
    /// </summary>
    /// <remarks>
    /// Each class needs to have a method for:
    /// - printing off data to the console for debugging
    /// - saving response data to memory and database
    /// - generating delete, post and put requests (even if nor applicable)
    /// </remarks>
    interface IRest
    {
        void print();
        void saveToMemory(HttpResponseMessage response);
        void saveToDB(int robotID);
        void saveAll(HttpResponseMessage response, int robotID);
        HttpRequestMessage deleteRequest();
        HttpRequestMessage postRequest();
        HttpRequestMessage putRequest();
    }
}
