fn main() -> i32 {
    let result = 0;
    if 5 == 5 { result = result + 1; };
    if 5 != 3 { result = result + 2; };
    if 3 < 5  { result = result + 4; };
    if 5 > 3  { result = result + 8; };
    if 5 <= 5 { result = result + 16; };
    if 3 <= 5 { result = result + 32; };
    if 5 >= 5 { result = result + 64; };
    if 5 >= 3 { result = result + 128; };
    result
}
