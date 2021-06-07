#include <stdio.h>

int main(int argc, char ** argv) {
	printf("Hello, world! argc=%d\n", argc);
	if (argc > 1) {
		printf("arguments: \n");
		int i;
		for (i = 0; i < argc; i++) printf("arg[%d] = \"%s\"\n", i, argv[i]);
	}
	printf("Going to do an open!\n");
	FILE *f = fopen("/test.c", "rb");
	printf("got ptr = %p\n", f);

	if (f) {
            char buf[1];
            printf("fread is going to happen!\n");
            fread(buf, 1, 1, f);
            printf("fread happened!\n");
            printf("read a byte = %c\n", buf[0]);
	} else {
		printf("Error opening file, ptr is NULL!\n");
	}
	printf("end of main!\n");
}

