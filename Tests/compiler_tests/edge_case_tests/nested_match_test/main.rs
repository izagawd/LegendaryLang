enum Outer {
    A,
    B
}
enum Inner {
    X,
    Y
}
fn main() -> i32 {
    let o = Outer.A;
    let i = Inner.Y;
    match o {
        Outer.A => match i {
            Inner.X => 1,
            Inner.Y => 2
        },
        Outer.B => 3
    }
}
