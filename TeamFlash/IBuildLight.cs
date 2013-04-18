using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TeamFlash
{
    interface IBuildLight
    {
        void Success();
        void Warning();
        void Fail();
        void Off();
    }
}
