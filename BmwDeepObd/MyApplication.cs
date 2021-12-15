﻿using System;
using AndroidX.AppCompat.App;

namespace BmwDeepObd
{
    [Android.App.Application(
        ResizeableActivity = true,
        LargeHeap = true,
        Name = ActivityCommon.AppNameSpace + ".DeepObd"
        )]
    // ReSharper disable once UnusedMember.Global
    public class MyApplication : Android.App.Application
    {
        public MyApplication(IntPtr handle, Android.Runtime.JniHandleOwnership ownerShip) : base(handle, ownerShip)
        {
        }

        // ReSharper disable once RedundantOverriddenMember
        public override void OnCreate()
        {
            base.OnCreate();
            AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
        }
    }
}
