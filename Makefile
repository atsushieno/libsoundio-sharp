
GEN_SOURCES = libsoundio-sharp/soundio.cs
PINVOKEGEN = external/nclang/samples/PInvokeGenerator/bin/Debug/PInvokeGenerator.exe
C_HEADERS = external/libsoundio/soundio/soundio.h

RUNTIME = mono --debug

all: libsoundio-sharp/bin/$(CONFIGURATION)/libsoundio-sharp.dll

libsoundio-sharp/bin/$(CONFIGURATION)/libsoundio-sharp.dll: $(GEN_SOURCES)
	msbuild

$(GEN_SOURCES): $(PINVOKEGEN) $(C_HEADERS)
	$(RUNTIME) $(PINVOKEGEN) --lib:soundio --ns:LibSoundIOSharp $(C_HEADERS) > $(GEN_SOURCES) || rm $(GEN_SOURCES)

$(PINVOKEGEN):
	cd external/nclang && msbuild

