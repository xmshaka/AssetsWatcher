﻿// Copyright (c) 2012-2015 Michael Stevenson <michael@mstevenson.net>
// This code is distributed under the MIT license

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;

namespace AssetsWatcher
{
	public class Watcher
	{
		public class FileEvent : UnityEvent<AssetFileInfo> {}
		public class FileMoveEvent : UnityEvent<AssetFileInfo, AssetFileInfo> {} // AssetFileInfo before and after the move

		internal static string[] allAssets;
		internal static List<Watcher> allWatchers;

		/// <summary>
		/// Occurs when an asset is first created.
		/// </summary>
		public readonly FileEvent onCreated = new FileEvent ();
		/// <summary>
		/// Occurs when an asset is deleted or is moved out of scope.
		/// </summary>
		public readonly FileEvent onDeleted = new FileEvent ();
		/// <summary>
		/// Occurs when the content of an asset is modified.
		/// </summary>
		public readonly FileEvent onModified = new FileEvent ();
		/// <summary>
		/// Occurs when an asset is renamed in-place.
		/// </summary>
		public readonly FileMoveEvent onRenamed = new FileMoveEvent ();
		/// <summary>
		/// Occurs when an asset is moved to a new location within scope.
		/// </summary>
		public readonly FileMoveEvent onMoved = new FileMoveEvent ();
		
		public readonly string basePath;
		public readonly UnityAssetType observedAssetTypes;
		public readonly bool recurseSubdirectories;

		/// <summary>
		/// Initialize the AssetsWatcher when a project is loaded.
		/// </summary>
		static Watcher ()
		{
			// Cache asset paths
			allAssets = AssetDatabase.GetAllAssetPaths ();
			allWatchers = new List<Watcher> ();
		}

		Watcher (string path, UnityAssetType assetType, bool recurseSubdirectories)
		{
			this.basePath = Path.Combine ("Assets", path);
			this.observedAssetTypes = assetType;
			this.recurseSubdirectories = recurseSubdirectories;
		}
		
		~Watcher ()
		{
			Watcher.RemoveObserver (this);
		}
		
		internal void InvokeEventForPaths (string[] paths, FileEvent e)
		{
			if (e == null)
				return;
			foreach (var p in paths) {
				if (IsValidPath (p)) {
					AssetFileInfo asset = new AssetFileInfo (p);
					if (observedAssetTypes == UnityAssetType.None || (observedAssetTypes & asset.Type) == asset.Type) {
						e.Invoke (asset);
					}
				}
			}
		}
		
		internal void InvokeMovedEventForPaths (Dictionary<string, string> paths, FileMoveEvent e)
		{
			if (e == null)
				return;
			foreach (var p in paths) {
				bool beforePathValid = IsValidPath (p.Value);
				bool afterPathValid = IsValidPath (p.Key);
				if (beforePathValid || afterPathValid) {
					var before = beforePathValid ? new AssetFileInfo (p.Value) : null;
					var after = afterPathValid ? new AssetFileInfo (p.Key) : null;
					e.Invoke (before, after);
				}
			}
		}
		
		/// <summary>
		/// Determines whether the specified assetPath is valid given the current path constraints.
		/// </summary>
		bool IsValidPath (string assetPath)
		{
			if (recurseSubdirectories)
				return assetPath.StartsWith (this.basePath);
			else
				return Path.GetDirectoryName (assetPath) == this.basePath;
		}


		#region API

		/// <summary>
		/// Watch for changes to the given asset type flags in the given path, and optionally recursing subdirectories.
		/// If no path is specified, the entire Assets folder will be used.
		/// If no asset type is specified, all asset types will be observed.
		/// </summary>
		public static Watcher Observe (string path = "", UnityAssetType assetType = UnityAssetType.None, bool recurseSubdirectories = true)
		{
			Watcher w = new Watcher (path, assetType, recurseSubdirectories);
			allWatchers.Add (w);
			return w;
		}
		
		/// <summary>
		/// Disable the specified watcher.
		/// </summary>
		public static void RemoveObserver (Watcher watcher)
		{
			allWatchers.Remove (watcher);
		}

		#endregion
	}
}