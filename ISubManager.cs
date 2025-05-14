using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public interface ISubManager
    {
        void Initialize(EAFManager manager);
        void Shutdown();
        void OnSave();
        void OnLoad();
        void Update();
    }
}
