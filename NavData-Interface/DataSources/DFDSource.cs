﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using NavData_Interface.Objects.Fix;
using AviationCalcUtilNet.GeoTools;

namespace NavData_Interface.DataSources
{
    public class DFDSource : IDataSource
    {
        private SQLiteConnection _connection;

        public DFDSource(string filePath)
        {
            _connection = new SQLiteConnection($"Data Source={filePath};Version=3;");
            _connection.Open();
        }

        public List<TerminalWaypoint> GetTerminalWaypoints(string identifier)
        {
            return GetObjects<TerminalWaypoint>("tbl_terminal_waypoints", "waypoint_identifier", identifier, reader =>
            {
                var waypoint = new TerminalWaypoint(
                    reader["waypoint_identifier"].ToString(),
                    new GeoPoint(
                        double.Parse(reader["waypoint_latitude"].ToString()),
                        double.Parse(reader["waypoint_longitude"].ToString())
                    ),
                    reader["area_code"].ToString(),
                    reader["icao_code"].ToString(),
                    reader["region_code"].ToString()
                );
                return waypoint;
            });
        }

        public List<Waypoint> GetEnrouteWaypoints(string identifier)
        {
            return GetObjects<Waypoint>("tbl_enroute_waypoints", "waypoint_identifier", identifier, reader =>
            {
                var waypoint = new Waypoint(
                    reader["waypoint_identifier"].ToString(),
                    new GeoPoint(
                        double.Parse(reader["waypoint_latitude"].ToString()),
                        double.Parse(reader["waypoint_longitude"].ToString())
                    ),
                    reader["area_code"].ToString(),
                    reader["icao_code"].ToString()
                );
                return waypoint;
            });
        }

        internal List<T> GetObjects<T>(string tableName, string keyColumn, string keyValue, Func<SQLiteDataReader, T> objectFactory)
        {
            var objects = new List<T>();

            using (var cmd = new SQLiteCommand(_connection))
            {
                cmd.CommandText = $"SELECT * FROM {tableName} WHERE {keyColumn} = @keyValue";
                cmd.Parameters.AddWithValue("@keyValue", keyValue);

                objects = GetObjectsWithQuery<T>(cmd, objectFactory);
            }

            return objects;
        }

        internal List<T> GetObjectsWithQuery<T>(SQLiteCommand cmd, Func<SQLiteDataReader, T> objectFactory)
        {
            var objects = new List<T>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var obj = objectFactory(reader);
                    objects.Add(obj);
                }
            }

            return objects;
        }

        List<Fix> IDataSource.GetFixesByIdentifier(string identifier)
        {
            List<Fix> foundFixes = new List<Fix>();

            foreach (var enrouteWaypoint in this.GetEnrouteWaypoints(identifier))
            {
                foundFixes.Add(enrouteWaypoint);
            }

            foreach (var terminalWaypoint in this.GetTerminalWaypoints(identifier))
            {
                foundFixes.Add(terminalWaypoint);
            }

            // TODO: also find Navaids

            return foundFixes;
        }
    }
}
