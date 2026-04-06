trait Foo {
    type Bruh;
    fn yo() -> Self.Bruh;
}

impl Foo for i32 {
    type Bruh = i32;
    fn yo() -> Bruh { 5 }
}

fn main() -> i32 {
    i32.yo()
}
