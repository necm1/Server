﻿using AltV.Net;
using AltV.Net.Async;
using Autofac;
using Microsoft.EntityFrameworkCore;
using PARADOX_RP.Core.Database;
using PARADOX_RP.Core.Interface;
using PARADOX_RP.Core.Module;
using PARADOX_RP.UI;
using PARADOX_RP.UI.Models;
using PARADOX_RP.UI.Windows.NativeMenu;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PARADOX_RP.Core.Factories
{
    internal class PXContainer : IDisposable
    {
        private readonly Type[] _loadedTypes = Assembly.GetExecutingAssembly().GetTypes();

        private IContainer _container;
        private ILifetimeScope _scope;

        private List<Type> _eventTypes = new List<Type>();
        private List<Type> _handlerTypes = new List<Type>();
        private List<Type> _moduleTypes = new List<Type>();
        private List<Type> _itemTypes = new List<Type>();
        private List<Type> _windowTypes = new List<Type>();
        private List<Type> _nativeMenuTypes = new List<Type>();

        internal void RegisterTypes()
        {
            var builder = new ContainerBuilder();

            LogStartup("Load types");
            LoadTypes();

            builder.RegisterType<Application>().As<IApplication>();
            builder.RegisterType<WindowManager>().As<IWindowManager>();

            LogStartup("Register events");
            foreach (var eventTarget in _eventTypes)
            {
                builder.RegisterTypes(eventTarget)
                .AsImplementedInterfaces()
                .SingleInstance();
            }

            LogStartup("Register handlers");
            foreach (var handler in _handlerTypes)
            {
                builder.RegisterTypes(handler)
                .AsImplementedInterfaces()
                .SingleInstance();
            }

            LogStartup("Register modules");
            foreach (var module in _moduleTypes)
            {
                builder.RegisterType(module)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Register windows");
            foreach (var window in _windowTypes)
            {
                builder.RegisterType(window)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            LogStartup("Register NativeMenus");
            foreach (var window in _nativeMenuTypes)
            {
                builder.RegisterType(window)
                .AsImplementedInterfaces()
                .AsSelf()
                .SingleInstance();
            }

            //LogStartup("Register itemscripts");
            //foreach (var item in _itemTypes)
            //{
            //    builder.RegisterType(item)
            //    .As<IItemScript>()
            //    .SingleInstance();
            //}

            //LogStartup("Register database context");
            var connection = "Server=localhost;Database=paradox;Uid=altv;Pwd=paradox_rp";
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<PXContext>()
                .UseMySql(connection, ServerVersion.AutoDetect(connection));


            builder.RegisterType<PXContext>()
               .WithParameter("options", dbContextOptionsBuilder.Options)
               .InstancePerLifetimeScope();

            _container = builder.Build();
        }

        internal void ResolveTypes()
        {
            _scope = _container.BeginLifetimeScope();
            foreach (var type in _moduleTypes)
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
                else if (IsWindowType(type))
                {
                    _windowTypes.Add(type);
                }
                else if (IsNativeMenuType(type))
                {
                    _nativeMenuTypes.Add(type);
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
                                            (type.BaseType == typeof(ModuleBase) ||
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

        private bool IsNativeMenuType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("PARADOX_RP.Game") &&
                                            type.IsAssignableTo<INativeMenu>() &&
                                            !type.Name.StartsWith("<");
        }

        private bool IsItemType(Type type)
        {
            if (type.Namespace == null) return false;
            return type.Namespace.StartsWith("GangRP_Server.Modules.Inventory.Item") &&
                                            !type.Name.StartsWith("<");
        }

        private static void LogStartup(string text)
        {
            AltAsync.Log($"[STARTUP] {text}");
        }

        public void Dispose()
        {
            _container.Dispose();
            _scope.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
