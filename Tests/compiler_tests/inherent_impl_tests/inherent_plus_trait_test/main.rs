trait Greet {
    fn greet(self: &Self) -> i32;
}

struct Foo {
    x: i32
}

impl Foo {
    fn double(self: &Self) -> i32 {
        self.x * 2
    }
}

impl Greet for Foo {
    fn greet(self: &Self) -> i32 {
        self.x
    }
}

fn main() -> i32 {
    let f = make Foo { x : 5 };
    f.double() + f.greet()
}
