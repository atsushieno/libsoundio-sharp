
CONFIGURATION = Debug

GEN_SOURCES = libsoundio-sharp/libsoundio-interop.cs
MANAGED_LIB = libsoundio-sharp/bin/$(CONFIGURATION)/libsoundio-sharp.dll
SHARED_LIB = external/libsoundio/libsoundio.dll
PINVOKEGEN = external/nclang/samples/PInvokeGenerator/bin/Debug/net462/PInvokeGenerator.exe
C_HEADERS = external/libsoundio/soundio/soundio.h

RUNTIME = mono --debug

ifeq ($(shell uname), Linux)
SHARED_LIB = external/libsoundio/libsoundio.so
else
ifeq ($(shell uname), Darwin)
SHARED_LIB = external/libsoundio/libsoundio.dylib
endif
endif


all: $(MANAGED_LIB)

$(MANAGED_LIB): $(GEN_SOURCES) $(SHARED_LIB)
	nuget restore
	msbuild

$(GEN_SOURCES): $(PINVOKEGEN) $(C_HEADERS)
	$(RUNTIME) $(PINVOKEGEN) --lib:soundio --ns:SoundIOSharp $(C_HEADERS) > $(GEN_SOURCES) || rm $(GEN_SOURCES)

$(PINVOKEGEN):
	cd external/nclang && msbuild

$(SHARED_LIB):
	cd external/libsoundio && cmake . && make
	cp $(SHARED_LIB) libsoundio-sharp/libs/

clean:
	msbuild /t:Clean
	cd external/nclang && msbuild /t:Clean
	cd external/libsoundio && make clean

