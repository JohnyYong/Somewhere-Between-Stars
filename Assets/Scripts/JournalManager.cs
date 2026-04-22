using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JournalManager : MonoBehaviour
{
	[Header("UI References")]
	public GameObject journalCanvas;
	public TMP_Text leftDayStamp;
	public TMP_Text leftEntryText;
	public TMP_Text rightDayStamp;
	public TMP_Text rightEntryText;
	public Button prevButton;
	public Button nextButton;
	private Button tearButton;

	[Header("Styling")]
	public Color normalInkColor = new Color(0.23f, 0.15f, 0.08f);
	public Color tornPageColor = new Color(0.6f, 0.5f, 0.4f, 0.5f);

	[Header("Audio")]
	public AudioClip[] writingSounds;       // 20 variants
	public AudioClip strikeSound;           // 1 strikethrough
	public AudioClip[] pageFlipSounds;      // 9 variants
	public AudioClip pickUpSound;           // 1 pick up book
	public float writingVolume = 0.3f;
	public float pageTurnVolume = 0.5f;
	public float pickUpVolume = 0.6f;

	[Header("Animation")]
	public float flipDuration = 0.3f;

	[System.Serializable]
	public class JournalEntry
	{
		public int dayNumber;
		public string displayContent;
		public bool isTorn;
	}

	private List<JournalEntry> _entries = new();
	private int _viewIndex = 0;
	private bool _isOpen = false;
	private bool _isFlipping = false;

	private List<CharData> _currentChars = new();
	private bool _lastCharWasSpace = false;

	private float _cursorBlinkTimer = 0f;
	private bool _cursorVisible = true;

	// Audio sources
	private AudioSource _journalAudio;      // writing sounds (center)
	private AudioSource _leftPageAudio;     // page turn panned left
	private AudioSource _rightPageAudio;    // page turn panned right

	private float _lastSoundTime = 0f;
	public float minSoundInterval = 0.08f;
	private int _lastWritingSoundIndex = -1;
	private int _lastFlipSoundIndex = -1;

	private struct CharData
	{
		public char character;
		public bool isStruck;
	}

	void Start()
	{
		LoadEntries();
		journalCanvas.SetActive(false);

		prevButton.onClick.AddListener(OnPrevPage);
		nextButton.onClick.AddListener(OnNextPage);
		//tearButton.onClick.AddListener(OnTearPage);

		// Center audio — writing sounds
		_journalAudio = gameObject.AddComponent<AudioSource>();
		_journalAudio.spatialBlend = 0f;
		_journalAudio.panStereo = 0f;
		_journalAudio.playOnAwake = false;

		// Left panned — going back (prev page)
		_leftPageAudio = gameObject.AddComponent<AudioSource>();
		_leftPageAudio.spatialBlend = 0f;
		_leftPageAudio.panStereo = -0.6f;
		_leftPageAudio.playOnAwake = false;

		// Right panned — going forward (next page)
		_rightPageAudio = gameObject.AddComponent<AudioSource>();
		_rightPageAudio.spatialBlend = 0f;
		_rightPageAudio.panStereo = 0.6f;
		_rightPageAudio.playOnAwake = false;
	}

	void Update()
	{
		if (!_isOpen) return;

		// Blinking cursor
		_cursorBlinkTimer += Time.deltaTime;
		if (_cursorBlinkTimer >= 0.5f)
		{
			_cursorBlinkTimer = 0f;
			_cursorVisible = !_cursorVisible;
			RefreshRightPage();
		}

		HandleTyping();
		UpdateNavigationButtons();
	}

	// ── Public API ──────────────────────────────

	public void OpenJournal()
	{
		_isOpen = true;
		journalCanvas.SetActive(true);

		_currentChars.Clear();
		_lastCharWasSpace = false;
		_cursorVisible = true;
		_cursorBlinkTimer = 0f;
		_lastSoundTime = 0f;
		_lastWritingSoundIndex = -1;
		_lastFlipSoundIndex = -1;

		_viewIndex = _entries.Count - 1;

		// Play pick up sound when journal opens
		if (pickUpSound != null)
			_journalAudio.PlayOneShot(pickUpSound, pickUpVolume);

		RefreshSpread();
	}

	public void CloseJournal()
	{
		if (!_isOpen) return;
		_isOpen = false;

		if (_currentChars.Count > 0)
			CommitCurrentEntry();

		journalCanvas.SetActive(false);
	}

	// ── Typing ──────────────────────────────────

	void HandleTyping()
	{
		if (_viewIndex != _entries.Count - 1) return;

		if (Input.GetKey(KeyCode.LeftShift) &&
			Input.GetKeyDown(KeyCode.Backspace))
		{
			HardDelete();
			RefreshRightPage();
			return;
		}

		foreach (char c in Input.inputString)
		{
			if (c == '\b')
			{
				HandleBackspace();
			}
			else if (c == '\n' || c == '\r')
			{
				if (!IsPageFull('\n'))
				{
					_currentChars.Add(new CharData
					{
						character = '\n',
						isStruck = false
					});
				}
				_lastCharWasSpace = false;
			}
			else
			{
				if (!IsPageFull(c))
				{
					_currentChars.Add(new CharData
					{
						character = c,
						isStruck = false
					});

					if (c == ' ')
					{
						PlayWritingSound();
						_lastCharWasSpace = true;
					}
					else
					{
						_lastCharWasSpace = false;
					}
				}
			}
		}

		RefreshRightPage();
	}

	bool IsPageFull(char nextChar)
	{
		_currentChars.Add(new CharData
		{
			character = nextChar,
			isStruck = false
		});

		rightEntryText.text = BuildSaveString();
		rightEntryText.ForceMeshUpdate();
		bool isOverflowing = rightEntryText.isTextOverflowing;
		_currentChars.RemoveAt(_currentChars.Count - 1);
		rightEntryText.text = BuildDisplayString();

		rightDayStamp.color = isOverflowing ?
			new Color(0.6f, 0.1f, 0.1f) : normalInkColor;

		return isOverflowing;
	}

	void HandleBackspace()
	{
		if (_currentChars.Count == 0) return;

		if (_lastCharWasSpace)
		{
			StrikeLastWord();
			_lastCharWasSpace = false;
		}
		else
		{
			_currentChars.RemoveAt(_currentChars.Count - 1);

			if (_currentChars.Count > 0)
				_lastCharWasSpace =
					_currentChars[_currentChars.Count - 1].character == ' ';
		}
	}

	void StrikeLastWord()
	{
		if (_currentChars.Count == 0) return;

		int i = _currentChars.Count - 1;

		while (i >= 0 && _currentChars[i].character == ' ')
			i--;

		while (i >= 0 &&
			   _currentChars[i].character != ' ' &&
			   _currentChars[i].character != '\n')
		{
			var c = _currentChars[i];
			c.isStruck = true;
			_currentChars[i] = c;
			i--;
		}

		PlayStrikeSound();
	}

	void HardDelete()
	{
		if (_currentChars.Count == 0) return;
		_currentChars.RemoveAt(_currentChars.Count - 1);

		if (_currentChars.Count > 0)
			_lastCharWasSpace =
				_currentChars[_currentChars.Count - 1].character == ' ';
	}

	// ── Audio ────────────────────────────────────

	void PlayWritingSound()
	{
		if (writingSounds == null || writingSounds.Length == 0) return;
		if (Time.time - _lastSoundTime < minSoundInterval) return;
		_lastSoundTime = Time.time;

		// Avoid repeating last sound
		int index;
		do
		{
			index = Random.Range(0, writingSounds.Length);
		} while (index == _lastWritingSoundIndex && writingSounds.Length > 1);

		_lastWritingSoundIndex = index;
		_journalAudio.PlayOneShot(writingSounds[index], writingVolume);
	}

	void PlayStrikeSound()
	{
		if (strikeSound == null) return;
		_journalAudio.PlayOneShot(strikeSound, writingVolume);
	}

	void PlayPageFlipSound(int direction)
	{
		if (pageFlipSounds == null || pageFlipSounds.Length == 0) return;

		// Avoid repeating last flip sound
		int index;
		do
		{
			index = Random.Range(0, pageFlipSounds.Length);
		} while (index == _lastFlipSoundIndex && pageFlipSounds.Length > 1);

		_lastFlipSoundIndex = index;

		// Direction -1 = going back = sound from left
		// Direction +1 = going forward = sound from right
		AudioSource source = direction < 0 ? _leftPageAudio : _rightPageAudio;
		source.PlayOneShot(pageFlipSounds[index], pageTurnVolume);
	}

	// ── Page Display ────────────────────────────

	void RefreshSpread()
	{
		RefreshLeftPage();
		RefreshRightPage();
		UpdateNavigationButtons();
	}

	void RefreshLeftPage()
	{
		int leftIndex = _viewIndex;

		if (leftIndex < 0 || leftIndex >= _entries.Count)
		{
			leftDayStamp.text = "";
			leftEntryText.text = "";
			return;
		}

		var entry = _entries[leftIndex];

		leftDayStamp.text = entry.dayNumber == 0 ?
							"A note from the developer" :
							$"Day {entry.dayNumber}";

		if (entry.isTorn)
		{
			leftEntryText.color = tornPageColor;
			leftEntryText.text = "<i>[this page was torn out]</i>";
		}
		else
		{
			leftEntryText.color = normalInkColor;
			leftEntryText.text = entry.displayContent;
		}
	}

	void RefreshRightPage()
	{
		bool isCurrentPage = _viewIndex == _entries.Count - 1;

		if (isCurrentPage)
		{
			int dayNumber = _viewIndex + 1;
			rightDayStamp.text = $"Day {dayNumber}";
			rightDayStamp.color = normalInkColor;
			rightEntryText.color = normalInkColor;
			rightEntryText.text = BuildDisplayString();
		}
		else
		{
			int rightIndex = _viewIndex + 1;

			if (rightIndex >= _entries.Count)
			{
				rightDayStamp.text = "";
				rightEntryText.text = "";
				return;
			}

			var entry = _entries[rightIndex];

			rightDayStamp.text = entry.dayNumber == 0 ?
								 "A note from the developer" :
								 $"Day {entry.dayNumber}";

			if (entry.isTorn)
			{
				rightEntryText.color = tornPageColor;
				rightEntryText.text = "<i>[this page was torn out]</i>";
			}
			else
			{
				rightEntryText.color = normalInkColor;
				rightEntryText.text = entry.displayContent;
			}
		}
	}

	string BuildDisplayString()
	{
		System.Text.StringBuilder sb = new();
		bool inStrike = false;

		foreach (var c in _currentChars)
		{
			if (c.isStruck && !inStrike)
			{
				sb.Append("<s>");
				inStrike = true;
			}
			else if (!c.isStruck && inStrike)
			{
				sb.Append("</s>");
				inStrike = false;
			}

			if (c.character == '\n')
				sb.Append('\n');
			else
				sb.Append(c.character);
		}

		if (inStrike) sb.Append("</s>");

		// Blinking cursor — only on writable page
		if (_cursorVisible && _viewIndex == _entries.Count - 1)
			sb.Append("<color=#3B2614>|</color>");

		return sb.ToString();
	}

	string BuildSaveString()
	{
		System.Text.StringBuilder sb = new();
		bool inStrike = false;

		foreach (var c in _currentChars)
		{
			if (c.isStruck && !inStrike)
			{
				sb.Append("<s>");
				inStrike = true;
			}
			else if (!c.isStruck && inStrike)
			{
				sb.Append("</s>");
				inStrike = false;
			}

			if (c.character == '\n')
				sb.Append('\n');
			else
				sb.Append(c.character);
		}

		if (inStrike) sb.Append("</s>");

		// No cursor — clean save
		return sb.ToString();
	}

	// ── Navigation ──────────────────────────────

	void OnPrevPage()
	{
		if (_viewIndex > 0 && !_isFlipping)
			StartCoroutine(FlipPage(-1));
	}

	void OnNextPage()
	{
		if (_viewIndex < _entries.Count - 1 && !_isFlipping)
			StartCoroutine(FlipPage(1));
	}

	IEnumerator FlipPage(int direction)
	{
		_isFlipping = true;

		var panel = journalCanvas.transform.GetChild(0);
		Vector3 originalScale = panel.localScale;

		// Play flip sound from correct side
		PlayPageFlipSound(direction);

		// Phase 1 — squish toward spine
		float elapsed = 0f;
		float half = flipDuration * 0.5f;

		while (elapsed < half)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / half;
			float scaleX = Mathf.Lerp(1f, 0f,
						   Mathf.SmoothStep(0f, 1f, t));
			panel.localScale = new Vector3(
				scaleX, originalScale.y, originalScale.z);
			yield return null;
		}

		// Swap content at midpoint
		_viewIndex += direction;
		RefreshSpread();

		// Phase 2 — expand back
		elapsed = 0f;
		while (elapsed < half)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / half;
			float scaleX = Mathf.Lerp(0f, 1f,
						   Mathf.SmoothStep(0f, 1f, t));
			panel.localScale = new Vector3(
				scaleX, originalScale.y, originalScale.z);
			yield return null;
		}

		panel.localScale = originalScale;
		_isFlipping = false;
	}

	void UpdateNavigationButtons()
	{
		prevButton.interactable = _viewIndex > 0 && !_isFlipping;
		nextButton.interactable = _viewIndex < _entries.Count - 1
								  && !_isFlipping;
	}

	//// ── Tear Page ───────────────────────────────

	//void OnTearPage()
	//{
	//	if (_currentChars.Count == 0) return;
	//	StartCoroutine(TearPageRoutine());
	//}

	//IEnumerator TearPageRoutine()
	//{
	//	_entries.Add(new JournalEntry
	//	{
	//		dayNumber = _entries.Count,
	//		displayContent = BuildSaveString(),
	//		isTorn = true
	//	});

	//	SaveEntries();

	//	var panel = journalCanvas.transform.GetChild(0);
	//	float elapsed = 0f;
	//	Vector3 origin = panel.localPosition;

	//	while (elapsed < 0.3f)
	//	{
	//		elapsed += Time.deltaTime;
	//		float shake = Mathf.Sin(elapsed * 60f) * 5f;
	//		panel.localPosition = origin + new Vector3(shake, 0, 0);
	//		yield return null;
	//	}

	//	panel.localPosition = origin;

	//	_currentChars.Clear();
	//	_lastCharWasSpace = false;
	//	_viewIndex = _entries.Count - 1;

	//	RefreshSpread();
	//}

	// ── Save / Load ─────────────────────────────

	void CommitCurrentEntry()
	{
		_entries.Add(new JournalEntry
		{
			dayNumber = _entries.Count,
			displayContent = BuildSaveString(),
			isTorn = false
		});

		Debug.Log($"Saved Day {_entries.Count - 1} entry");
		SaveEntries();
	}

	void SaveEntries()
	{
		PlayerPrefs.SetInt("JournalCount", _entries.Count);

		for (int i = 0; i < _entries.Count; i++)
		{
			PlayerPrefs.SetInt($"Journal_Day_{i}",
								_entries[i].dayNumber);
			PlayerPrefs.SetString($"Journal_Content_{i}",
								   _entries[i].displayContent);
			PlayerPrefs.SetInt($"Journal_Torn_{i}",
								_entries[i].isTorn ? 1 : 0);
		}

		PlayerPrefs.Save();
	}

	void LoadEntries()
	{
		_entries.Clear();
		int count = PlayerPrefs.GetInt("JournalCount", 0);

		if (count == 0)
		{
			_entries.Add(new JournalEntry
			{
				dayNumber = 0,
				displayContent =
					"Hey, you.\n" +
					"I don't know your name. " +
					"I don't know what brought you here, " +
					"or what you left behind to board this train.\n" +
					"But I'm glad you're here.\n" +
					"Outside that window is everything " +
					"I ever dreamed of as a child - " +
					"planets so big they make your heart ache, " +
					"stars so distant they feel like old friends " +
					"you haven't met yet.\n" +
					"I wanted to share that with someone.\n" +
					"So here we are.\n" +
					"You, me, and an infinite stretch of dark " +
					"beautiful nothing between us.\n" +
					"The train won't stop. " +
					"Time out here moves differently. " +
					"Let it.\n" +
					"Rest your eyes on the horizon. " +
					"Write something true in these pages. " +
					"Let the lamp sway. " +
					"Let the stars pass.\n" +
					"You don't have to be anywhere else " +
					"right now.\n" +
					"This seat was always yours.\n" +
					"— SaigouSan, " +
					"a guy who built a train to travel across the stars to you.",
				isTorn = false
			});

			SaveEntries();
			return;
		}

		for (int i = 0; i < count; i++)
		{
			_entries.Add(new JournalEntry
			{
				dayNumber = PlayerPrefs.GetInt(
								 $"Journal_Day_{i}", i),
				displayContent = PlayerPrefs.GetString(
								 $"Journal_Content_{i}", ""),
				isTorn = PlayerPrefs.GetInt(
								 $"Journal_Torn_{i}", 0) == 1
			});
		}
	}
}