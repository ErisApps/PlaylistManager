﻿using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using static BeatSaberMarkupLanguage.Components.CustomListTableData;
using System.Reflection;
using HMUI;
using PlaylistManager.HarmonyPatches;
using PlaylistManager.Utilities;
using PlaylistManager.Interfaces;
using UnityEngine;
using PlaylistManager.Configuration;
using BeatSaberPlaylistsLib.Types;

namespace PlaylistManager.UI
{
    class AddPlaylistController: IPlaylistManagerModal
    {
        private StandardLevelDetailViewController standardLevelDetailViewController;
        private AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController;

        [UIComponent("list")]
        public CustomListTableData customListTableData;

        [UIComponent("modal")]
        private readonly ModalView modal;

        [UIComponent("root")]
        private readonly RectTransform rootTransform;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        [UIComponent("keyboard")]
        private readonly RectTransform keyboardTransform;

        private BeatSaberPlaylistsLib.Types.IPlaylist[] loadedplaylists;
        internal bool parsed;

        AddPlaylistController(StandardLevelDetailViewController standardLevelDetailViewController, AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController)
        {
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
            parsed = false;
        }

        internal void Parse()
        {
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "PlaylistManager.UI.Views.AddPlaylist.bsml"), standardLevelDetailViewController.transform.Find("LevelDetail").gameObject, this);
        }

        internal void ShowPlaylists()
        {
            modal.Show(true);
            customListTableData.data.Clear();
            loadedplaylists = PlaylistLibUtils.playlistManager.GetAllPlaylists(true);

            foreach (BeatSaberPlaylistsLib.Types.IPlaylist playlist in loadedplaylists)
            {
                if (playlist is IDeferredSpriteLoad && !((IDeferredSpriteLoad)playlist).SpriteWasLoaded)
                {
                    IDeferredSpriteLoad deferredSpriteLoadPlaylist = (IDeferredSpriteLoad)playlist;
                    _ = playlist.coverImage;
                    deferredSpriteLoadPlaylist.SpriteLoaded -= DeferredSpriteLoadPlaylist_SpriteLoaded;
                    deferredSpriteLoadPlaylist.SpriteLoaded += DeferredSpriteLoadPlaylist_SpriteLoaded;
                }
                else
                {
                    ShowPlaylist(playlist);
                }
            }

            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
        }

        private void DeferredSpriteLoadPlaylist_SpriteLoaded(object sender, EventArgs e)
        {
            if (sender is IDeferredSpriteLoad deferredSpriteLoad)
            {
                ShowPlaylist((BeatSaberPlaylistsLib.Types.IPlaylist)sender);
                customListTableData.tableView.ReloadData();
                ((IDeferredSpriteLoad)sender).SpriteLoaded -= DeferredSpriteLoadPlaylist_SpriteLoaded;
            }
        }

        private void ShowPlaylist(BeatSaberPlaylistsLib.Types.IPlaylist playlist)
        {
            string subName = string.Format("{0} songs", playlist.beatmapLevelCollection.beatmapLevels.Length);
            if (Array.Exists(playlist.beatmapLevelCollection.beatmapLevels, level => level.levelID == standardLevelDetailViewController.selectedDifficultyBeatmap.level.levelID))
            {
                subName += " (contains song)";
            }
            customListTableData.data.Add(new CustomCellInfo(playlist.collectionName, subName, playlist.coverImage));
        }

        [UIAction("select-cell")]
        internal void OnCellSelect(TableView tableView, int index)
        {
            loadedplaylists[index].Add(standardLevelDetailViewController.selectedDifficultyBeatmap.level);
            customListTableData.tableView.ClearSelection();
            if (annotatedBeatmapLevelCollectionsViewController.isActiveAndEnabled && AnnotatedBeatmapLevelCollectionsViewController_SetData.isCustomBeatmapLevelPack)
            {
                annotatedBeatmapLevelCollectionsViewController.SetData(AnnotatedBeatmapLevelCollectionsViewController_SetData.otherCustomBeatmapLevelCollections, annotatedBeatmapLevelCollectionsViewController.selectedItemIndex, false);
            }
            PlaylistLibUtils.playlistManager.StorePlaylist(loadedplaylists[index]);
            modal.Hide(true);
        }

        [UIAction("keyboard-enter")]
        internal void CreatePlaylist(string playlistName)
        {
            if (!PluginConfig.Instance.DefaultImageDisabled)
            {
                PlaylistLibUtils.CreatePlaylist(playlistName, PluginConfig.Instance.AuthorName);
            }
            else
            {
                PlaylistLibUtils.CreatePlaylist(playlistName, PluginConfig.Instance.AuthorName, "");
            }
            ShowPlaylists();
        }

        public void ParentControllerDeactivated()
        {
            if (parsed && rootTransform != null && modalTransform != null && keyboardTransform != null)
            {
                modalTransform.transform.SetParent(rootTransform);
                keyboardTransform.transform.SetParent(modalTransform);
            }
        }
    }
}
