﻿// Copyright (c) Richasy. All rights reserved.

using RodelAgent.Models.Common;

namespace RodelAgent.Context;

/// <summary>
/// 音频数据服务.
/// </summary>
public sealed class AudioDataService(string workingDir, string packageDir)
    : MetadataServiceBase<AudioMeta>(workingDir, packageDir, "audio.sqlite")
{
    public async Task<List<string>> GetAllSessionsAsync()
    {
        return await Task.Run(async () =>
        {
            using var sql = GetSql();
            var list = await sql.Queryable<AudioMeta>().ToListAsync().ConfigureAwait(false);
            return list.ConvertAll(p => p.Value);
        }).ConfigureAwait(false);
    }

    public async Task BatchAddSessionsAsync(List<AudioMeta> metadataList)
    {
        await Task.Run(async () =>
        {
            using var sql = GetSql();
            await sql.Insertable(metadataList).ExecuteCommandAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<AudioMeta?> GetSessionAsync(string id)
    {
        return await Task.Run(async () =>
        {
            using var sql = GetSql();
            return await sql.Queryable<AudioMeta>().Where(p => p.Id == id).FirstAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task AddOrUpdateSessionAsync(AudioMeta metadata)
    {
        await Task.Run(async () =>
        {
            using var sql = GetSql();
            await sql.Storageable(metadata).ExecuteCommandAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    protected override async Task InitializeDbContextAsync(string path)
    {
        await Task.Run(() =>
        {
            using var sql = GetSql();
            sql.CodeFirst.InitTables<AudioMeta>();
        }).ConfigureAwait(false);
    }

    public async Task RemoveSessionAsync(string id)
    {
        await Task.Run(async () =>
        {
            using var sql = GetSql();
            await sql.Deleteable<AudioMeta>().Where(p => p.Id == id).ExecuteCommandAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
