trait Converter {
    let Output :! type;
    fn convert(val: i32) -> (Self as Converter).Output;
}

impl Converter for i32 {
    let Output :! type = i32;
    fn convert(val: i32) -> (Self as Converter).Output {
        val + 1
    }
}

fn main() -> i32 {
    i32.convert(41)
}
