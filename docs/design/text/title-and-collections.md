# Text: the title screen and the collections at home (M3-09)

> Author: the story & flow session, for ui to port into the localization tables. English is the source; Vietnamese is
> natural, not word for word. Spec: `docs/features/M3-09-title-collections.md`.
>
> **Key names.** The tables already use a `ui.` prefix (`ui.title.wordmark`, `ui.pause.*`), so M3-09's `title.*`
> keys are written here as `ui.title.*`.

## The title screen
| Key | English | Tiếng Việt | Note |
|---|---|---|---|
| `ui.title.subtitle` | a radio left on | một chiếc radio vẫn bật | M3-09's line, kept. Small caps in English; plain lower case in Vietnamese, where small caps fight the diacritics. |
| `ui.title.continue` | Continue | Tiếp tục | |
| `ui.title.new_game` | New Game | Trò chơi mới | same as `ui.pause.new_game` today |
| `ui.title.settings` | Settings | Cài đặt | |
| `ui.title.quit` | Quit | Thoát | |
| `ui.title.new_game_question` | Put this moon away and start again? | Cất mặt trăng này đi và bắt đầu lại? | M3-09's confirm. It may replace `ui.pause.new_game_question`, which leaves the pause menu. |
| `ui.title.new_game_aside` | Your old moon is kept. | Mặt trăng cũ vẫn được giữ nguyên. | |
| `ui.title.new_game_keep` | Keep this moon | Giữ mặt trăng này | the safe answer, focused |
| `ui.title.new_game_confirm` | Start again | Bắt đầu lại | |
| `ui.pause.quit_to_title` | Quit to title | Về màn hình chính | replaces the pause menu's New game |

## The crew plates on the memory shelf
Each crew bay's plate is in that crew member's own handwriting. Names stay as they are in every language; only the
small words around them change.

| Key | English | Tiếng Việt |
|---|---|---|
| `crew.ro.plate` | Ro, on air ♪ | Ro, đang lên sóng ♪ |
| `crew.theo.plate` | Theo & the seedlings | Theo & lũ cây con |
| `crew.ines.plate` | I. Kalani, Lead | I. Kalani, Trưởng nhóm |
| `crew.kenji.plate` | Kenji (ask the duck) | Kenji (hỏi chú vịt) |

If Be Vietnam Pro has no ♪ (U+266A), ui drops it rather than shipping a missing-glyph box.

## "A set is filling up" (once per crew, at the set's second item)
| Key | English | Tiếng Việt |
|---|---|---|
| `ticker.set.ro.filling` | Ro's shelf is filling up. It already sounds like her. | Kệ của Ro đầy dần. Nghe đã thấy giống cô ấy rồi. |
| `ticker.set.theo.filling` | Theo's shelf is filling up. Something on it is almost growing. | Kệ của Theo đầy dần. Có món gì trên đó như sắp nảy mầm. |
| `ticker.set.ines.filling` | Ines's shelf is filling up, everything in neat rows. | Kệ của Ines đầy dần, món nào cũng xếp thẳng hàng. |
| `ticker.set.kenji.filling` | Kenji's shelf is filling up. The duck approves. | Kệ của Kenji đầy dần. Chú vịt gật gù tán thành. |

## A set completed (optional, with `CrewSetCompleted`)
M3-09 lights the crew member's window and plays their last log (`docs/design/text/crew-logs.md`). If gameplay wants
one line before the log, use these.

| Key | English | Tiếng Việt |
|---|---|---|
| `ticker.set.ro.complete` | Ro's window is lit again. | Cửa sổ của Ro lại sáng đèn. |
| `ticker.set.theo.complete` | Theo's window is lit again. | Cửa sổ của Theo lại sáng đèn. |
| `ticker.set.ines.complete` | Ines's window is lit again. | Cửa sổ của Ines lại sáng đèn. |
| `ticker.set.kenji.complete` | Kenji's window is lit again. | Cửa sổ của Kenji lại sáng đèn. |

## The other collections' words
- **Bell's sign** (all 8 tapes): "Lumen After Dark", the show's name, untranslated in every language, as
  `radio.channel.lumen_after_dark` is today.
- **Ines's map:** the ✓ note is in `docs/design/text/crew-map.md` (`map.complete.note`).
