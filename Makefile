
CONFIGURATION = Debug

GEN_SOURCES = libsoundio-sharp/libsoundio-interop.cs
MANAGED_LIB = libsoundio-sharp/bin/$(CONFIGURATION)/libsoundio-sharp.dll
SHARED_LIB = external/libsoundio/libsoundio.so
PINVOKEGEN = external/nclang/samples/PInvokeGenerator/bin/Debug/PInvokeGenerator.exe
C_HEADERS = external/libsoundio/soundio/soundio.h

RUNTIME = mono --debug


all: $(MANAGED_LIB)

$(MANAGED_LIB): $(GEN_SOURCES) $(SHARED_LIB)
	msbuild

$(GEN_SOURCES): $(PINVOKEGEN) $(C_HEADERS)
	$(RUNTIME) $(PINVOKEGEN) --lib:soundio --ns:LibSoundIOSharp $(C_HEADERS) > $(GEN_SOURCES) || rm $(GEN_SOURCES)

$(PINVOKEGEN):
	cd external/nclang && msbuild

$(SHARED_LIB):
	cd external/libsoundio && cmake . && make


clean:
	msbuild /t:Clean
	cd external/nclang && msbuild /t:Clean
	cd external/libsoundio && make clean

