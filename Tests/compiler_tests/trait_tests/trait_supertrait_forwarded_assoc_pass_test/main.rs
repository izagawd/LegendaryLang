trait Converter(T:! type) {
    let Output :! type;
}

trait IntConverter(T:! type): Converter(T, Output = i32) {}

impl Converter(bool) for i32 {
    let Output :! type = i32;
}

impl IntConverter(bool) for i32 {}

fn main() -> i32 {
    42
}
