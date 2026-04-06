trait Maker {
    type Product: Copy;
    fn make(input: Self) -> i32;
}

struct NonCopy {
    val: i32
}

struct Foo {
    val: i32
}

impl Maker for Foo {
    type Product = NonCopy;
    fn make(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
