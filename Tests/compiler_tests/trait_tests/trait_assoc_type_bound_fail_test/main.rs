trait Maker: Sized {
    let Product :! Sized +Copy;
    fn make(input: Self) -> i32;
}

struct NonCopy {
    val: i32
}

struct Foo {
    val: i32
}

impl Maker for Foo {
    let Product :! Sized = NonCopy;
    fn make(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
