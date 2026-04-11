use Std.Ops.Add;
fn add_twice(T:! Sized +Add(T, Output = T) + Copy, a: T) -> T {
    a + a
}

fn main() -> i32 {
    add_twice(i32, 21)
}
