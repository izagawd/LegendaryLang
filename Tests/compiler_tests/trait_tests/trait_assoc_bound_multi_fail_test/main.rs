trait Processor {
    type Input: Copy;
    type Output: Copy;
    fn process(input: Self) -> i32;
}

struct NonCopy {
    val: i32
}

struct Foo {
    val: i32
}

impl Copy for Foo {}

impl Processor for Foo {
    type Input = i32;
    type Output = NonCopy;
    fn process(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
