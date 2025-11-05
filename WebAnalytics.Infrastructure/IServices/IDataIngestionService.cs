using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAnalytics.Infrastructure.IServices
{
    public interface IDataIngestionService
    {
        Task<bool> IngestFromJsonFilesAsync(string gaFilePath, string psiFilePath);
    }

}
