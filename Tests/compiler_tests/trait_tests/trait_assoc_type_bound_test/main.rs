trait Maker: Sized {
    let Product :! Copy;
    fn make(input: Self) -> i32;
}

struct Foo {
    val: i32
}

impl Maker for Foo {
    let Product :! Copy = i32;
    fn make(input: Foo) -> i32 {
        input.val
    }
}

fn main() -> i32 {
    let f = make Foo { val : 7 };
    (Foo as Maker).make(f)
}
