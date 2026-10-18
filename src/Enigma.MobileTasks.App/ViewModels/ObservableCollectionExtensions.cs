using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Enigma.MobileTasks.App.ViewModels;

/// <summary>
/// Keeps a collection of ViewModels in step with the model objects they show.
/// </summary>
public static class ObservableCollectionExtensions
{
    extension<TViewModel>(ObservableCollection<TViewModel> target)
    {
        /// <summary>
        /// Brings the collection into the order of <paramref name="source"/>, in place: a ViewModel
        /// whose model is still there is kept (moved if needed, then updated), a new model gets a new
        /// ViewModel, and a ViewModel whose model is gone is removed.
        /// </summary>
        /// <typeparam name="TModel">The model type, compared by reference.</typeparam>
        /// <param name="source">The models, in the order they should be shown.</param>
        /// <param name="modelOf">The model a ViewModel shows.</param>
        /// <param name="create">Builds the ViewModel of a new model.</param>
        /// <param name="update">Refreshes a kept ViewModel.</param>
        /// <remarks>
        /// In place rather than cleared and refilled, because a kept ViewModel keeps its view — and
        /// with it the focus of a text box and the phone's keyboard, which a rebuild would close after
        /// every item added.
        /// </remarks>
        public void SyncWith<TModel>(
            IReadOnlyList<TModel> source,
            Func<TViewModel, TModel> modelOf,
            Func<TModel, TViewModel> create,
            Action<TViewModel> update)
            where TModel : class
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(modelOf);
            ArgumentNullException.ThrowIfNull(create);
            ArgumentNullException.ThrowIfNull(update);

            for (int index = 0; index < source.Count; index++)
            {
                TModel model = source[index];
                int found = -1;

                for (int candidate = index; candidate < target.Count; candidate++)
                {
                    if (ReferenceEquals(modelOf(target[candidate]), model))
                    {
                        found = candidate;
                        break;
                    }
                }

                if (found < 0)
                {
                    target.Insert(index, create(model));
                    continue;
                }

                if (found != index)
                {
                    target.Move(found, index);
                }

                update(target[index]);
            }

            while (target.Count > source.Count)
            {
                target.RemoveAt(target.Count - 1);
            }
        }
    }
}
