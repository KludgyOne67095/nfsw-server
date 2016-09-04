﻿using NHibernate;
using OfflineServer.Servers.Database;
using OfflineServer.Servers.Database.Entities;
using System;

namespace OfflineServer
{
    /// <summary>
    /// Class containing all of the required variables and initializers to create and use a Server Engine.
    /// </summary>
    /// <remarks>This is NOT to be confused with the NFS:W Game Engines. Server Engines are custom prices and patches.</remarks>
    public class Engine
    {
        public Achievements Achievements = new Achievements();

        public static class PersonaManagement
        {
            private static ISession session;
            static PersonaManagement()
            {
                session = SessionManager.getSessionFactory().OpenSession();
            }

            public static PersonaEntity persona
            {
                get
                {
                    if (Access.CurrentSession.ActivePersona != null)
                        return session.Load<PersonaEntity>(Access.CurrentSession.ActivePersona.Id);
                    return new PersonaEntity(); // to ensure no NullReferenceExceptions, it's just a dummy
                }
                set
                {
                    if (persona != value && value != null)
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            PersonaEntity personaEntity = session.Load<PersonaEntity>(Access.CurrentSession.ActivePersona.Id);
                            personaEntity = value;
                            session.Update(personaEntity);
                            transaction.Commit();
                        }
                    }
                }
            }

            public static Int32 getDefaultPersonaIdx()
            {
                return session.Load<UserEntity>(1).defaultPersonaIdx;
            }
            public static void setDefaultPersonaIdx(Int32 defaultPersonaIdx)
            {
                using (var transaction = session.BeginTransaction())
                {
                    UserEntity userEntity = session.Load<UserEntity>(1);
                    userEntity.defaultPersonaIdx = defaultPersonaIdx;
                    session.Update(userEntity);
                    transaction.Commit();
                }
            }
        }
    }
}