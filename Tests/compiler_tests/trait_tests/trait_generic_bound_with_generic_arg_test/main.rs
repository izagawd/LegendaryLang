use Std.Core.Ops.Add;
fn add_them[T:! Add(T, Output = T) + Copy](one: T, two: T) -> T {
    one + two
}

fn main() -> i32 {
    add_them(2, 3)
}
