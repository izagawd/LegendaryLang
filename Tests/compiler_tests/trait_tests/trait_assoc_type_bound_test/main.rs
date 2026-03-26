trait Maker {
    type Product: Copy;
    fn make(input: Self) -> i32;
}

struct Foo {
    val: i32
}

impl Maker for Foo {
    type Product = i32;
    fn make(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    let f = Foo { val = 7 };
    <Foo as Maker>::make(f)
}
