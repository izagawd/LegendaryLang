trait Foo {
    type Bruh;
    fn yo() -> Bruh;
}

impl Foo for i32 {
    type Bruh = i32;
    fn yo() -> i32 { 5 }
}

fn main() -> i32 {
    i32.yo()
}
