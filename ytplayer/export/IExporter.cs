using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.export {
    public interface IExporter {
        Task<bool> Export();
        void DeleteResult();
        void Cancel();
    }
}
