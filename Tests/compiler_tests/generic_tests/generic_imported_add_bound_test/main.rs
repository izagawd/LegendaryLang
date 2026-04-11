use Std.Ops.Add;

fn add_things[T:! Sized +Add(T, Output = T) + Copy](a: T, b: T) -> T {
    a + b
}

fn main() -> i32 {
    add_things(20, 22)
}
