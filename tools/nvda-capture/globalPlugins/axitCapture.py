# Axit speech capture (docs/axit/automation.md, tier 3): what NVDA speaks, and when it cancels speech, written to
# %TEMP%\axit-nvda-speech.log, one line each with the time, only while %TEMP%\axit-nvda-capture.on exists, which the
# test runner makes for its run and removes after it. Nothing the user hears in daily use is recorded.

import os
import time

import globalPluginHandler
import speech
from speech import extensions

FLAG = os.path.join(os.environ.get("TEMP", "."), "axit-nvda-capture.on")
LOG = os.path.join(os.environ.get("TEMP", "."), "axit-nvda-speech.log")


def _write(kind, text):
	if not os.path.exists(FLAG):
		return
	try:
		with open(LOG, "a", encoding="utf-8") as log:
			log.write("%.3f\t%s\t%s\n" % (time.time(), kind, text.replace("\n", " ")))
	except OSError:
		pass


def _onSpeech(speechSequence=None, *args, **kwargs):
	text = " ".join(item for item in (speechSequence or []) if isinstance(item, str)).strip()
	if text:
		_write("speak", text)


def _onCancel(*args, **kwargs):
	_write("cancel", "")


class GlobalPlugin(globalPluginHandler.GlobalPlugin):

	def __init__(self):
		super().__init__()
		self._original = None
		if hasattr(extensions, "pre_speech"):
			extensions.pre_speech.register(_onSpeech)
		else:
			# Before the extension point: the speak function wrapped.
			self._original = speech.speech.speak

			def speak(speechSequence, *args, **kwargs):
				_onSpeech(speechSequence)
				return self._original(speechSequence, *args, **kwargs)

			speech.speech.speak = speak
			speech.speak = speak
		if hasattr(extensions, "speechCanceled"):
			extensions.speechCanceled.register(_onCancel)

	def terminate(self):
		if hasattr(extensions, "pre_speech"):
			extensions.pre_speech.unregister(_onSpeech)
		elif self._original is not None:
			speech.speech.speak = self._original
			speech.speak = self._original
		if hasattr(extensions, "speechCanceled"):
			extensions.speechCanceled.unregister(_onCancel)
		super().terminate()
