struct Foo { val: i32 }
impl Copy for Foo {}

impl Foo {
    fn consume(self: Self) -> i32 {
        self.val
    }
}

fn main() -> i32 {
    let foo = make Foo { val: 5 };
    foo.consume() + foo.consume()
}
