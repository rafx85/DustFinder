using System.Windows;
using System.Windows.Controls;
using HearthDb;
using Hearthstone_Deck_Tracker.Controls;
using HdtCard = Hearthstone_Deck_Tracker.Hearthstone.Card;

namespace DustFinder.Plugin.Controls;

/// <summary>
/// Exposes HDT's ordinary CardImage.CardId property as a dependency property so
/// card IDs can safely be supplied by a WPF data template.
/// </summary>
public sealed class BindableCardImage : ContentControl
{
	private readonly CardImage _cardImage;

	public static readonly DependencyProperty BoundCardIdProperty = DependencyProperty.Register(
		nameof(BoundCardId),
		typeof(string),
		typeof(BindableCardImage),
		new PropertyMetadata(null, OnBoundCardIdChanged));

	public BindableCardImage()
	{
		_cardImage = new CardImage
		{
			ShowQuestionmark = true,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Content = _cardImage;
	}

	public string? BoundCardId
	{
		get => (string?)GetValue(BoundCardIdProperty);
		set => SetValue(BoundCardIdProperty, value);
	}

	private static void OnBoundCardIdChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if(dependencyObject is not BindableCardImage image)
			return;

		var cardId = args.NewValue as string;
		if(string.IsNullOrWhiteSpace(cardId) || !Cards.All.TryGetValue(cardId, out var card))
		{
			image._cardImage.SetCardIdFromCard(null);
			return;
		}

		image._cardImage.SetCardIdFromCard(new HdtCard(card, false));
	}
}
