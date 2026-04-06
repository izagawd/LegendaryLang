trait Converter(T:! type) {
    type Output;
}

trait IntConverter(T:! type): Converter(T, Output = i32) {}

impl Converter(bool) for i32 {
    type Output = bool;
}

impl IntConverter(bool) for i32 {}

fn main() -> i32 {
    5
}
