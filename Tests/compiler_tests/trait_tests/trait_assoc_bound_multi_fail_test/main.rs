trait Processor: Sized {
    let Input :! Sized +Copy;
    let Output :! Sized +Copy;
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
    let Input :! Sized = i32;
    let Output :! Sized = NonCopy;
    fn process(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
