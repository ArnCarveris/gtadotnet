#include "stdafx.h"
#include "Script.h"
#include "Log.h"
#include "InternalCalls.h"
#include "GTAUtils.h"
#include "ScriptProcessor.h"

using namespace GTA::Internal;
using namespace System::Threading;

namespace GTA {
	/*void TickScript::Run() {
		while (true) {
			ScriptContext^ context = ScriptContext::current;

			// lalala, ticking the member function
			OnTick();

			context->_wakeUpAt = GTAUtils::GetGameTimer() + _interval;
			context->_continue->Set();
			context->_execute->WaitOne();
		}
	}*/

	void BaseScript::BindKey(Windows::Forms::Keys key, Action^ function) {
		if (_boundKeys == nullptr) {
			_boundKeys = gcnew List<BoundKeyData>();
		}

		BoundKeyData data;
		data._keyCode = (int)key;
		data._call = function;

		_boundKeys->Add(data);
	}

	void BaseScript::ProcessKeyBindings() {
		if (_boundKeys == nullptr) {
			_boundKeys = gcnew List<BoundKeyData>();
		}

		for each (BoundKeyData binding in _boundKeys) {
			if (ScriptProcessor::Instance->_keys[binding._keyCode]) {
				binding._call();
			}
		}
	}

	void BaseScript::Run() {

	}

	void BaseScript::Wait(int time) {
		ScriptContext^ context = ScriptContext::current;

		Function::Call(0x0001, 0);

		context->_wakeUpAt = GTAUtils::GetGameTimer() + time;
		context->_continue->Set();
		context->_execute->WaitOne();
	}

	ScriptContext::ScriptContext(BaseScript^ myScript) {
		_wakeUpAt = 0;
		_endNext = false;
		_execute = gcnew EventWaitHandle(false, EventResetMode::AutoReset);
		_continue = gcnew EventWaitHandle(false, EventResetMode::AutoReset);
		_myScript = myScript;
		_identifier = myScript->GetType()->Name;

		_thread = gcnew Thread(gcnew ThreadStart(this, &ScriptContext::Run)); // it needs strange instantiation stuff
		_thread->Start();
	}

	void ScriptContext::Clean() {
		_thread->Abort();
	}

	void ScriptContext::WakeUp() {
		DWORD currentTime = GTAUtils::GetGameTimer();

		if (currentTime >= _wakeUpAt) {
			// execute the script
			current = this;
			_execute->Set();
			// the script runs now. we'll wait until it says it can return.
			// we take it the script sets its _wakeUpAt itself.
			_continue->WaitOne();
			current = nullptr;
		}
	}

	void ScriptContext::Run() {
		Thread::CurrentThread->CurrentCulture = gcnew Globalization::CultureInfo("en-US");

		// wait for first initialization attempt
		_execute->WaitOne();

		// run the script's OnStart event
		try {
			_myScript->OnStart();

			// we should wait a tick first before running the main script loop
			// commented out (2010-01-23): why is this needed?
			// re-enabled (2010-01-25): this gives all scripts the chance to OnStart before other scripts go 'round.
			_continue->Set();
			_execute->WaitOne();

			try {
				_myScript->Run(); // the script should loop itself
			} catch (Exception^ e) {
				HandleException(e);
			}
		} catch (Exception^ e) {
			HandleException(e);
		} finally {
			_continue->Set(); // if the script returned, we should still continue
			_endNext = true; // to clean up after ourselves
		}
	}

	void ScriptContext::HandleException(Exception^ e) {
		Log::Error("Unhandled exception thrown by script " + this->_myScript->GetType()->Name + ": \n" + e->ToString());
	}
}