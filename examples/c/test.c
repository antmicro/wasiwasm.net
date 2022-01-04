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
            while (1) {
                char buf[128];
                int nread = fread(buf, 1, 128, f);
                for (int j = 0; j < nread; j++) printf("%c", buf[j]);
                if (nread < 128) break;
            }
	} else {
		printf("Error opening file, ptr is NULL!\n");
	}
        printf("\n ================ \n");
	printf("end of main!\n");
}

