﻿using AltV.Net;
using AltV.Net.Async;
using Autofac;
using Autofac.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Interface;
using PARADOX_RP.Core.Models;
using PARADOX_RP.Core.Module;
using PARADOX_RP.Game.Inventory.Interfaces;
using PARADOX_RP.Game.Phone.Interfaces;
using PARADOX_RP.UI;
using PARADOX_RP.UI.Models;
using PARADOX_RP.UI.Windows.NativeMenu;
using PARADOX_RP.Utils;
using PARADOX_RP.Utils.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PARADOX_RP.Core.Factories
{
    internal class PXContainer : IDisposable
    {
        private readonly Type[] _loadedTypes = Assembly.GetExecutingAssembly().GetTypes();

        private IContainer _container;
        private ILifetimeScope _scope;

        private List<Type> _handlerTypes = new List<Type>();
        private List<Type> _moduleTypes = new List<Type>();
        private List<Type> _itemTypes = new List<Type>();
        private List<Type> _windowTypes = new List<Type>();
        private List<Type> _nativeMenuTypes = new List<Type>();
        private List<Type> _phoneAppTypes = new List<Type>();
        private List<Type> _singletonTypes = new List<Type>();

        internal void RegisterTypes()
        {
            var builder = new ContainerBuilder();
            var stopwatch = Stopwatch.StartNew();

            LogStartup("Loading applications...");
            LoadTypes();

            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<WindowController>().As<IWindowController>();
            builder.RegisterType<Logger>().As<ILogger>();

            LogStartup("Loading controllers...");
            foreach (var handler in _handlerTypes)
            {
                builder.RegisterTypes(handler)
                .AsImplementedInterfaces()
                .SingleInstance();
            }

            LogStartup("Loading game modules...");
            foreach (var module in _moduleTypes)
            {
                builder.RegisterType(module)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Loading UI Windows...");
            foreach (var window in _windowTypes)
            {
                builder.RegisterType(window)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Loading static native menus...");
            foreach (var window in _nativeMenuTypes)
            {
                builder.RegisterType(window)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Loading phone applications...");
            foreach (var app in _phoneAppTypes)
            {
                builder.RegisterType(app)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Loading singletons...");
            foreach (var singleton in _singletonTypes)
            {
                builder.RegisterType(singleton)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Loading itemscripts...");
            foreach (var item in _itemTypes)
            {
                builder.RegisterType(item)
                .As<IItemScript>()
               .SingleInstance();
            }

            //LogStartup("Register database context");
            string connection = $"Server=localhost; port=3306; Database=altv-paradox_rp; UserId=root; Password=lCvLpEGKhvz4WDsN;";
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<PXContext>()
                .UseMySql(connection, ServerVersion.AutoDetect(connection));


            builder.RegisterType<PXContext>()
               .WithParameter("options", dbContextOptionsBuilder.Options)
               .InstancePerLifetimeScope();

            _container = builder.Build();
            stopwatch.Stop();

            LogStartup($"Successfully build server container in {stopwatch.ElapsedMilliseconds} ms!");
        }

        internal void ResolveTypes()
        {
            _scope = _container.BeginLifetimeScope();
            
            foreach (var type in _moduleTypes)
            {
                var stopwatch = Stopwatch.StartNew();
                _scope.Resolve(type);
                stopwatch.Stop();

                LogStartup($"Module > {type.Name} resolved in {stopwatch.ElapsedMilliseconds} ms");
            }

            foreach (var type in _singletonTypes)
            {
                _scope.Resolve(type);
            }
 
            foreach (var type in _windowTypes)
            {
                _scope.Resolve(type);
            }

            foreach (var type in _nativeMenuTypes)
            {
                _scope.Resolve(type);
            }

            foreach (var type in _phoneAppTypes)
            {
                _scope.Resolve(type);
            }
        }

        internal T Resolve<T>()
        {
            return _scope.Resolve<T>();
        }

        private void LoadTypes()
        {
            foreach (Type type in _loadedTypes)
            {
                if (IsHandlerType(type))
                {
                    _handlerTypes.Add(type);
                }
                else if (IsModuleType(type))
                {
                    _moduleTypes.Add(type);
                }
                else if (IsItemType(type))
                {
                    _itemTypes.Add(type);
                }
                else if (IsSingletonType(type))
                {
                    _singletonTypes.Add(type);
                }
                else if (IsWindowType(type))
                {
                    _windowTypes.Add(type);
                }
                else if (IsNativeMenuType(type))
                {
                    _nativeMenuTypes.Add(type);
                }
                else if (IsPhoneAppType(type))
                {
                    _phoneAppTypes.Add(type);
                }
            }
        }

        private bool IsHandlerType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Controllers") &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsModuleType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Game") &&
                                            type.BaseType != null &&
                                            (type.BaseType == typeof(Module.ModuleBase) ||
                                            type.BaseType.IsGenericType) &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsWindowType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.UI") &&
                                            type.BaseType != null &&
                                            (type.BaseType == typeof(Window) ||
                                            type.BaseType.IsGenericType) &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsSingletonType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP") &&
                                            type.BaseType != null &&
                                            type.IsAssignableTo<ISingleton>() &&
                                            !type.IsGenericType &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsNativeMenuType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Game") &&
                                            type.IsAssignableTo<INativeMenu>() &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsPhoneAppType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Game.Phone.Content") &&
                                            type.BaseType != null &&
                                            type.IsAssignableTo<IPhoneApplication>() &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsItemType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Game.Inventory.Content") &&
                                            !type.Name.StartsWith("<");
        }

        private static void LogStartup(string text)
        {
            AltAsync.Log($"[+] Load >> {text}");
        }

        public void Dispose()
        {
            _container.Dispose();
            _scope.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
