struct Foo {
    x: i32,
    y: i32
}

impl Copy for Foo {}

impl Foo {
    fn sum(self: &Self) -> i32 {
        self.x + self.y
    }
}

fn main() -> i32 {
    let b: Box(Foo) = Box.New(make Foo { x: 10, y: 32 });
    b.sum()
}
