﻿#nullable enable
using System;
using System.Threading.Tasks;
using Couchbase.Client.Transactions.Components;
using Couchbase.Core.Compatibility;

#pragma warning disable CS1591

namespace Couchbase.Client.Transactions.Internal.Test
{
    /// <summary>
    /// Protected hooks purely for testing purposes.  If you're an end-user looking at these for any reason, then
    /// please contact us first about your use-case: we are always open to adding good ideas into the transactions
    /// library.
    /// </summary>
    /// <remarks>All methods have default no-op implementations.</remarks>
    [InterfaceStability(Level.Volatile)]
    internal interface ITestHooks
    {


        public Task<int?> BeforeAtrCommit(AttemptContext self);
        public Task<int?> AfterAtrCommit(AttemptContext self);
        public Task<int?> BeforeDocCommitted(AttemptContext self, string id);
        public Task<int?> BeforeDocRolledBack(AttemptContext self, string id);
        public Task<int?> AfterDocCommittedBeforeSavingCas(AttemptContext self, string id);
        public Task <int?> BeforeDocChangedDuringCommit(AttemptContext self, string id);
        public Task<int?> AfterDocCommitted(AttemptContext self, string id);
        public Task<int?> AfterDocsCommitted( AttemptContext self);
        public Task<int?> BeforeDocRemoved(AttemptContext self, string id);
        public Task<int?> AfterDocRemovedPreRetry( AttemptContext self, string id);
        public Task<int?> AfterDocRemovedPostRetry(AttemptContext self, string id);
        public Task<int?> AfterDocsRemoved(AttemptContext self);
        public Task<int?> BeforeAtrPending(AttemptContext self);
        public Task<int?> AfterAtrPending(AttemptContext self);
        public Task<int?> BeforeAtrComplete(AttemptContext self);
        public Task<int?> AfterAtrComplete(AttemptContext self);
        public Task<int?> BeforeAtrRolledBack(AttemptContext self);
        public Task<int?> AfterAtrRolledBack(AttemptContext self);
        public Task<int?> AfterGetComplete(AttemptContext self, string id);
        public Task<int?> BeforeRollbackDeleteInserted(AttemptContext self, string id);
        public Task<int?> AfterStagedReplaceComplete(AttemptContext self, string id);
        public Task<int?> AfterStagedRemoveComplete(AttemptContext self, string id);
        public Task<int?> BeforeStagedInsert(AttemptContext self, string id);
        public Task<int?> BeforeStagedRemove(AttemptContext self, string id);
        public Task<int?> BeforeStagedReplace(AttemptContext self, string id);
        public Task<int?> AfterStagedInsertComplete(AttemptContext self, string id);
        public Task<int?> BeforeGetAtrForAbort( AttemptContext self);
        public Task<int?> BeforeAtrAborted(AttemptContext self);
        public Task<int?> AfterAtrAborted(AttemptContext self);
        public Task<int?> AfterRollbackReplaceOrRemove( AttemptContext self, string id);
        public Task<int?> AfterRollbackDeleteInserted(AttemptContext self, string id);
        public Task<int?> BeforeRemovingDocDuringStagedInsert(AttemptContext self);
        public Task<int?> BeforeCheckAtrEntryForBlockingDoc(AttemptContext self, string id);
        public Task<int?> BeforeDocGet(AttemptContext self, string id);
        public Task<int?> BeforeGetDocInExistsDuringStagedInsert(AttemptContext self, string id);
        public Task<int?> BeforeAtrCommitAmbiguityResolution(AttemptContext self);
        public Task<int?> BeforeQuery(AttemptContext self, string statement);
        public Task<int?> AfterQuery(AttemptContext self, string statement);
        public Task<int?> BeforeOverwritingStagedInsertRemoval( AttemptContext self, string id);
        public Task<int?> BeforeRemoveStagedInsert( AttemptContext self, string id);
        public Task<int?> AfterRemoveStagedInsert(AttemptContext self, string id);
        public bool HasExpiredClientSideHook(AttemptContext self, string place, string? docId);
        public Task<string?> AtrIdForVBucket(AttemptContext self, int vBucketId);
    }

    /// <summary>
    /// Implementation of ITestHooks that relies on default interface implementation.
    /// </summary>
    internal class DefaultTestHooks : ITestHooks
    {
        public static readonly ITestHooks Instance = new DefaultTestHooks();
        public const string HOOK_ROLLBACK = "rollback";
        public const string HOOK_GET = "get";
        public const string HOOK_INSERT = "insert";
        public const string HOOK_REPLACE = "replace";
        public const string HOOK_REMOVE = "remove";
        public const string HOOK_BEFORE_COMMIT = "commit";
        public const string HOOK_ABORT_GET_ATR = "abortGetAtr"; // No references in Java code.
        public const string HOOK_ROLLBACK_DOC = "rollbackDoc";
        public const string HOOK_DELETE_INSERTED = "deleteInserted";
        public const string HOOK_CREATE_STAGED_INSERT = "createdStagedInsert";
        public const string HOOK_INSERT_QUERY = "insertQuery";
        public const string HOOK_COMMIT_DOC = "commitDoc";
        public const string HOOK_QUERY = "query";
        public const string HOOK_ATR_COMMIT = "atrCommit";
        public const string HOOK_ATR_COMMIT_AMBIGUITY_RESOLUTION = "atrCommitAmbiguityResolution";
        public const string HOOK_ATR_ABORT = "atrAbort";
        public const string HOOK_ATR_ROLLBACK_COMPLETE = "atrRollbackComplete";
        public const string HOOK_ATR_PENDING = "atrPending";
        public const string HOOK_ATR_COMPLETE = "atrComplete";
        public const string HOOK_CHECK_WRITE_WRITE_CONFLICT = "checkATREntryForBlockingDoc";
        public const string HOOK_BEFORE_QUERY = "beforeQuery";
        public const string HOOK_AFTER_QUERY = "afterQuery";
        public const string HOOK_QUERY_BEGIN_WORK = "queryBeginWork";
        public const string HOOK_QUERY_COMMIT = "queryCommit";
        public const string HOOK_QUERY_KV_GET = "queryKvGet";
        public const string HOOK_QUERY_KV_REPLACE = "queryKvReplace";
        public const string HOOK_QUERY_KV_REMOVE = "queryKvRemove";
        public const string HOOK_QUERY_KV_INSERT = "queryKvInsert";
        public const string HOOK_REMOVE_DOC = "removeDoc";
        public const string HOOK_BEFORE_DOC_CHANGED_DURING_COMMIT = "beforeDocChangedDuringCommit";
        public const string HOOK_QUERY_ROLLBACK = "queryRollback";
        public virtual Task<int?> BeforeAtrCommit(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterAtrCommit(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeDocCommitted(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeDocRolledBack(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocCommittedBeforeSavingCas(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeDocChangedDuringCommit(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocCommitted(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocsCommitted( AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeDocRemoved(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocRemovedPreRetry( AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocRemovedPostRetry(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterDocsRemoved(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeAtrPending(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterAtrPending(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeAtrComplete(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterAtrComplete(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeAtrRolledBack(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterAtrRolledBack(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterGetComplete(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeRollbackDeleteInserted(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterStagedReplaceComplete(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterStagedRemoveComplete(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeStagedInsert(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeStagedRemove(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeStagedReplace(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterStagedInsertComplete(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeGetAtrForAbort( AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeAtrAborted(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterAtrAborted(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterRollbackReplaceOrRemove( AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterRollbackDeleteInserted(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeRemovingDocDuringStagedInsert(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeCheckAtrEntryForBlockingDoc(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeDocGet(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeGetDocInExistsDuringStagedInsert(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeAtrCommitAmbiguityResolution(AttemptContext self) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeQuery(AttemptContext self, string statement) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterQuery(AttemptContext self, string statement) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeOverwritingStagedInsertRemoval( AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> BeforeRemoveStagedInsert( AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual Task<int?> AfterRemoveStagedInsert(AttemptContext self, string id) => Task.FromResult<int?>(0);
        public virtual bool HasExpiredClientSideHook(AttemptContext self, string place, string? docId) => false;
        public virtual Task<string?> AtrIdForVBucket(AttemptContext self, int vBucketId) => Task.FromResult<string?>(null); }

    /// <summary>
    /// Implementation of ITestHooks that allows individual delegates per hook.
    /// </summary>
    internal class DelegateTestHooks : DefaultTestHooks
    {
        private Func<AttemptContext, string, Task<int?>> BeforeDocGetImpl { get; set; } = DefaultTestHooks.Instance.BeforeDocGet;
        public override Task<int?> BeforeDocGet(AttemptContext self, string id) => BeforeDocGetImpl(self, id);

        private Func<AttemptContext, string, Task<int?>> BeforeDocCommittedImpl { get; set; } =
            DefaultTestHooks.Instance.BeforeDocCommitted;
        public override Task<int?>  BeforeDocCommitted(AttemptContext self, string id) => BeforeDocCommittedImpl(self, id);

        private Func<AttemptContext, Task<int?>> BeforeAtrCommitImpl { get; set; } =
            DefaultTestHooks.Instance.BeforeAtrCommit;
        public override Task<int?> BeforeAtrCommit(AttemptContext self) => BeforeAtrCommitImpl(self);

        private Func<AttemptContext, string, Task<int?>> AfterStagedReplaceCompleteImpl { get; set; } =
            DefaultTestHooks.Instance.AfterStagedReplaceComplete;

        public override Task<int?> AfterStagedReplaceComplete(AttemptContext self, string id) =>
            AfterStagedReplaceCompleteImpl(self, id);
    }
}


/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2021 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
