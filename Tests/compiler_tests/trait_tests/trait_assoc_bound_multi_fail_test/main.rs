trait Processor {
    let Input :! Copy;
    let Output :! Copy;
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
    let Input :! type = i32;
    let Output :! type = NonCopy;
    fn process(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
