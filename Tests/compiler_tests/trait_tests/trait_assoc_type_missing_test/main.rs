trait Maker {
    type Product;
    fn make(input: Self) -> i32;
}

struct Foo {
    val: i32
}

impl Maker for Foo {
    fn make(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    5
}
