trait Converter(Target:! Sized): Sized {
    fn convert(input: Self) -> Target;
}

impl Converter(bool) for i32 {
    fn convert(input: i32) -> bool {
        true
    }
}

fn main() -> i32 {
    5
}
