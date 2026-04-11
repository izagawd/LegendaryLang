trait Converter(T:! Sized) {
    let Output :! Sized;
}

trait IntConverter(T:! Sized): Converter(T, Output = i32) {}

impl Converter(bool) for i32 {
    let Output :! Sized = i32;
}

impl IntConverter(bool) for i32 {}

fn main() -> i32 {
    42
}
